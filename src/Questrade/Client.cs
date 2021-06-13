using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using PollQT.DataTypes;
using PollQT.Questrade.Responses;
using PollQT.Scheduling;
using PollQT.Util;
using Serilog;
namespace PollQT.Questrade
{
    internal class Client
    {
        private Token AuthToken { get; set; }
        private HttpClient HttpClient { get; set; }
        private readonly ITokenStore tokenStore;
        private readonly ILogger log;

        // the API sometimes throws 429 Too Many Requests if you go too fast, so irrespective of any
        // external scheduling, allow only 1 request per second 
        private readonly IScheduler minimumRateLimiter = new RateScheduler(1, TimeSpan.FromSeconds(1));

        private enum RequestType
        {
            TOKEN,
            ACCOUNTS,
            ACCOUNT_BALANCES,
            ACCOUNT_POSITIONS,
        }
        public Client(Context context) {
            log = context.Logger.ForContext<Client>();
            HttpClient = new HttpClient();
            AuthToken = new Token();
            log.Information("workDir: {workDir}", context.WorkDir);
            if (!Directory.Exists(context.WorkDir)) {
                log.Information("Created {workDir}", context.WorkDir);
                Directory.CreateDirectory(context.WorkDir);
            }
            tokenStore = new FileTokenStore(context);
            log.Information("tokenStore: {tokenStore}", tokenStore);
            NewToken(tokenStore.GetToken(), supressWrite: true);
        }
        private string RequestRoute(RequestType requestType, string? id = default) => requestType switch
        {
            RequestType.TOKEN =>
                $"oauth2/token?grant_type=refresh_token&refresh_token={AuthToken.RefreshToken}",
            RequestType.ACCOUNTS => "v1/accounts",
            RequestType.ACCOUNT_BALANCES => $"v1/accounts/{id}/balances",
            RequestType.ACCOUNT_POSITIONS => $"v1/accounts/{id}/positions",
            _ => throw new NotImplementedException()
        };
        private string RequestBaseAddress(RequestType requestType) => requestType switch
        {
            RequestType.TOKEN => "https://login.questrade.com/",
            // if ApiServer isn't set, we need a new token, throw Unauthorized so we relogin
            _ => AuthToken.ApiServer ?? throw new UnauthorizedException()
        };
        private void NewToken(Token authToken, bool supressWrite = false) {
            AuthToken = authToken;
            HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {authToken.AccessToken}");
            if (!supressWrite) {
                tokenStore.WriteToken(authToken);
            }
        }

        private async Task<string> MakeRequest(RequestType type, CancellationToken cancelToken, string? id = default, uint loginRetries = 1) {
            minimumRateLimiter.WaitUntilRunnable();

            var stopwatch = Stopwatch.StartNew();
            var route = RequestBaseAddress(type) + RequestRoute(type, id);
            var resp = await HttpClient.GetAsync(route);
            stopwatch.Stop();
            log.Debug("Request: {request}", resp.RequestMessage);
            log.Debug("Response: {resp}", resp);
            if (!resp.IsSuccessStatusCode) {                
                if (resp.StatusCode == HttpStatusCode.Unauthorized && loginRetries > 0) {
                    // don't invoke any retry logic if we've been cancelled
                    if (cancelToken.IsCancellationRequested) throw new TaskCanceledException("MakeRequest Unauthorized Handler");
                    log.Warning("Unuthorized: {type} ({dur}ms)", type, stopwatch.ElapsedMilliseconds);
                    // We get unauthorized often -- let's not throw an exception, log in here and retry
                    await Login(cancelToken);
                    // Login will throw if it fails so we can retry here worry-free
                    return await MakeRequest(type, cancelToken, id);
                } else if (resp.StatusCode == HttpStatusCode.TooManyRequests) {
                    if (cancelToken.IsCancellationRequested) throw new TaskCanceledException("MakeRequest TooManyRequests Handler");
                    try {
                        var nextReset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(resp.Headers.GetValues("X-RateLimit-Reset").First()));
                        minimumRateLimiter.RequestDelayUntil(nextReset);
                        log.Warning("Too Many Requests - waiting until rate reset at {dt}", nextReset);
                        return await MakeRequest(type, cancelToken, id);
                    } catch (Exception e) {
                        throw new ApiException("Told TooManyRequests but not told when we can make more", e);
                    }
                } else {
                    log.Error("Error: {type} {resp} ({dur}ms)", type, resp, stopwatch.ElapsedMilliseconds);
                    throw resp.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => new UnauthorizedException("Remained unauthorized after retry."),
                        _ => new UnexpectedStatusException(resp),
                    };
                }
            } else {
                log.Information("Success: {type} ({dur}ms)", type, stopwatch.ElapsedMilliseconds);
                return await resp.Content.ReadAsStringAsync();
            }
        }
        private async Task Login(CancellationToken cancelToken) {
            if (cancelToken.IsCancellationRequested) throw new TaskCanceledException("Login");
            log.Information("Logging in with refresh token");
            AuthToken = tokenStore.GetToken(); // get latest refresh in case it changed
            var authToken = await MakeRequest(RequestType.TOKEN, cancelToken);
            log.Verbose("Body: {@res}", authToken);
            NewToken(Token.FromJson(authToken));
            log.Information("Successfully logged in with refresh token");
        }
        private async Task<T> GetResponse<T>(RequestType type, CancellationToken cancelToken, string? id = default) where T : JsonSerializable<T> {
            if (cancelToken.IsCancellationRequested) throw new TaskCanceledException("GetResponse");
            var res = await MakeRequest(type, cancelToken, id);
            log.Verbose("Body: {@res}", res);
            var resObj = JsonSerializable<T>.FromJson(res);
            log.Debug("{resName}: {@res}", resObj.GetType(), resObj);
            return resObj;
        }
        private async Task<List<PollResult>> Poll(CancellationToken cancelToken) {
            if (AuthToken.ApiServer is null || AuthToken.AccessToken is null) {
                await Login(cancelToken);
            }
            var resp = new List<PollResult>();
            var accounts = await GetResponse<AccountsResponse>(RequestType.ACCOUNTS, cancelToken);
            var timestamp = DateTimeOffset.UtcNow;
            foreach (var account in accounts.Accounts) {
                var accountBalances = await GetResponse<AccountBalancesResponse>(RequestType.ACCOUNT_BALANCES, cancelToken, account.Number);
                var accountPositions = await GetResponse<AccountPositionsResponse>(RequestType.ACCOUNT_POSITIONS, cancelToken, account.Number);
                resp.Add(new PollResult(timestamp, account, accountBalances.CombinedBalances[0], accountPositions.Positions));
            }
            return resp;
        }
        public async Task<List<PollResult>> PollWithRetry(int maxRetries = 10) {
            var backoff = new ExponentialBackoff(maxRetries, delayMilliseconds: 200, maxDelayMilliseconds: 120000);
        retry:
            var cts = new CancellationTokenSource();
            var awaitable = Poll(cts.Token);
            try {
                var result = await awaitable.TimeoutAfter(TimeSpan.FromSeconds(60));
                return result;
            } catch (Exception e) {
                if (e is ApiException) {
                    log.Error(e, "API issue - backing off to retry in {delay} ms.", backoff.NextDelay);
                    await backoff.Delay();
                    goto retry;
                } else if (e is TimeoutException) {
                    cts.Cancel(); // signal cancellation -- mainly so we don't mess with stored tokens out-of-band

                    // Continue tracking it on the thread pool but abandon the results
                    _ = awaitable.ContinueWith((t) => {
                        log.Warning(t.Exception, "Information from task that was abandoned due to timeout: Status {status} Result {result}", t.Status, t.Result);
                    });

                    log.Error(e, "Timed out - backing off to retry in {delay} ms.", backoff.NextDelay);
                    await backoff.Delay();
                    goto retry;
                } else {
                    throw;
                }
            } finally {
                cts.Dispose();
            }
        }
    }
}
