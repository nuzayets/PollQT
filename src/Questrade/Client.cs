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
        private Market? MarketInfo { get; set; }

        // the API sometimes throws 429 Too Many Requests if you go too fast
        private readonly IScheduler minimumRateLimiter;

        private enum RequestType
        {
            TOKEN,
            ACCOUNTS,
            ACCOUNT_BALANCES,
            ACCOUNT_POSITIONS,
            ACCOUNT_ACTIVITIES,
            MARKETS,
        }
        public Client(Context context)
        {
            log = context.Logger.ForContext<Client>();
            HttpClient = new HttpClient();
            AuthToken = new Token();
            log.Information("workDir: {workDir}", context.WorkDir);
            if (!Directory.Exists(context.WorkDir))
            {
                log.Information("Created {workDir}", context.WorkDir);
                Directory.CreateDirectory(context.WorkDir);
            }
            tokenStore = new FileTokenStore(context);
            log.Information("tokenStore: {tokenStore}", tokenStore);
            NewToken(tokenStore.GetToken(), supressWrite: true);
            minimumRateLimiter = new RateScheduler(context, 5, TimeSpan.FromSeconds(1));
        }
        private string RequestRoute(RequestType requestType, string? id = default) => requestType switch
        {
            RequestType.TOKEN =>
                $"oauth2/token?grant_type=refresh_token&refresh_token={AuthToken.RefreshToken}",
            RequestType.ACCOUNTS => "v1/accounts",
            RequestType.ACCOUNT_BALANCES => $"v1/accounts/{id}/balances",
            RequestType.ACCOUNT_POSITIONS => $"v1/accounts/{id}/positions",
            RequestType.ACCOUNT_ACTIVITIES => $"v1/accounts/{id}/activities",
            RequestType.MARKETS => "v1/markets",
            _ => throw new NotImplementedException()
        };
        private string RequestBaseAddress(RequestType requestType) => requestType switch
        {
            RequestType.TOKEN => "https://login.questrade.com/",
            // if ApiServer isn't set, we need a new token, throw Unauthorized so we relogin
            _ => AuthToken.ApiServer ?? throw new UnauthorizedException()
        };

        private static string RequestQuery(RequestType requestType) => requestType switch
        {
            RequestType.ACCOUNT_ACTIVITIES => $"?startTime={DateTimeOffset.Now.AddDays(-7):o}&endTime={DateTimeOffset.Now:o}",
            _ => ""
        };

        private void NewToken(Token authToken, bool supressWrite = false)
        {
            AuthToken = authToken;
            HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {authToken.AccessToken}");
            if (!supressWrite)
            {
                tokenStore.WriteToken(authToken);
            }
        }

        private async Task<string> MakeRequest(RequestType type, CancellationToken cancelToken, string? id = default, string? queryOverride = default)
        {   
            minimumRateLimiter.WaitUntilRunnable();

            var stopwatch = Stopwatch.StartNew();
            var route = RequestBaseAddress(type) + RequestRoute(type, id) + (queryOverride ?? RequestQuery(type));
            var resp = await HttpClient.GetAsync(route);
            stopwatch.Stop();
            log.Debug("Request: {request}", resp.RequestMessage);
            log.Debug("Response: {resp}", resp);
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // don't invoke any retry logic if we've been cancelled
                    if (cancelToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException("MakeRequest Unauthorized Handler");
                    }

                    log.Warning("Unuthorized: {type} ({dur}ms)", type, stopwatch.ElapsedMilliseconds);
                    // We get unauthorized often -- let's not throw an exception, log in here and retry
                    await Login(cancelToken);
                    // Login will throw if it fails so we can retry here worry-free
                    return await MakeRequest(type, cancelToken, id);
                }
                else if (resp.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException("MakeRequest TooManyRequests Handler");
                    }

                    try
                    {
                        var nextReset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(resp.Headers.GetValues("X-RateLimit-Reset").First()));
                        minimumRateLimiter.RequestDelayUntil(nextReset);
                        log.Warning("Too Many Requests - waiting until rate reset at {dt}", nextReset);
                        return await MakeRequest(type, cancelToken, id);
                    }
                    catch (Exception e)
                    {
                        throw new ApiException("Told TooManyRequests but not told when we can make more", e);
                    }
                }
                else
                {
                    log.Error("Error: {type} {resp} ({dur}ms)", type, resp, stopwatch.ElapsedMilliseconds);
                    throw resp.StatusCode switch
                    {
                        _ => new UnexpectedStatusException(resp),
                    };
                }
            }
            else
            {
                log.Information("Success: {type} ({dur}ms)", type, stopwatch.ElapsedMilliseconds);
                return await resp.Content.ReadAsStringAsync();
            }
        }
        private async Task Login(CancellationToken cancelToken)
        {
            if (cancelToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("Login");
            }

            log.Information("Logging in with refresh token");
            AuthToken = tokenStore.GetToken(); // get latest refresh in case it changed
            var authToken = await MakeRequest(RequestType.TOKEN, cancelToken);
            log.Verbose("Body: {@res}", authToken);
            NewToken(Token.FromJson(authToken));
            log.Information("Successfully logged in with refresh token");
        }
        private async Task<T> GetResponse<T>(RequestType type, CancellationToken cancelToken, string? id = default) where T : JsonSerializable<T>
        {
            if (cancelToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("GetResponse");
            }

            var res = await MakeRequest(type, cancelToken, id);
            log.Verbose("Body: {@res}", res);
            var resObj = JsonSerializable<T>.FromJson(res);
            log.Debug("{resName}: {@res}", resObj.GetType(), resObj);
            return resObj;
        }
        
        private async Task CheckLogin(CancellationToken cancelToken) {
            if (AuthToken.ApiServer is null || AuthToken.AccessToken is null)
            {
                await Login(cancelToken);
            }   
        }
        
        private async Task<List<PollResult>> Poll(CancellationToken cancelToken)
        {
            await CheckLogin(cancelToken);
            var accounts = await GetResponse<AccountsResponse>(RequestType.ACCOUNTS, cancelToken);
            var timestamp = DateTimeOffset.UtcNow;
            var resp = new List<PollResult>();
            foreach (var account in accounts.Accounts)
            {
                var accountBalances = await GetResponse<AccountBalancesResponse>(RequestType.ACCOUNT_BALANCES, cancelToken, account.Number);
                var accountPositions = await GetResponse<AccountPositionsResponse>(RequestType.ACCOUNT_POSITIONS, cancelToken, account.Number);
                var accountActivities = await GetResponse<AccountActivitiesResponse>(RequestType.ACCOUNT_ACTIVITIES, cancelToken, account.Number);
                resp.Add(new PollResult(timestamp, account, accountBalances.CombinedBalances[0], accountPositions.Positions, accountActivities.Activities));
            }
            return resp;
        }

        public async Task<List<(Account account, List<AccountActivity> activities)>> BackfillActivities(CancellationToken cancelToken)
        {
            await Login(cancelToken);
            var accounts = await GetResponse<AccountsResponse>(RequestType.ACCOUNTS, cancelToken);
            var resp = new List<(Account account, List<AccountActivity>)>();
            foreach (var account in accounts.Accounts)
            {
                var activities = new List<AccountActivity>();
                var searchStart = new DateTimeOffset(DateTime.Today.Year - 5, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
                while (searchStart < DateTimeOffset.Now)
                {
                    var searchEnd = searchStart.AddMonths(1).AddDays(-1);
                    var query = $"?startTime={searchStart:o}&endTime={searchEnd:o}";
                    var queryRes = await MakeRequest(RequestType.ACCOUNT_ACTIVITIES, cancelToken, account.Number, queryOverride: query);
                    var resObj = JsonSerializable<AccountActivitiesResponse>.FromJson(queryRes);
                    activities.AddRange(resObj.Activities);
                    searchStart = searchStart.AddMonths(1);
                }

                resp.Add((account, activities));
            }
            return resp;
        }

        public async Task<bool> MarketOpenDelay(CancellationToken token)
        {
            await CheckLogin(token);
            if (MarketInfo == null || MarketInfo.StartTime.Date < DateTimeOffset.Now.Date)
            {
                var marketsResponse = await GetResponse<MarketsInfo>(RequestType.MARKETS, token);
                MarketInfo = marketsResponse.Markets.Where(m => m.Name == "TSX").First();
                MarketInfo.StartTime = MarketInfo.StartTime.AddMinutes(-1);
                MarketInfo.EndTime = MarketInfo.EndTime.AddMinutes(1);
                if (MarketInfo.StartTime.Date < DateTimeOffset.Now.Date)
                {
                    var startTimeToday = DateTimeOffset.Now.Date + (MarketInfo.StartTime - MarketInfo.StartTime.Date);
                    if (startTimeToday > DateTimeOffset.Now)
                    {
                        var ts = startTimeToday - DateTimeOffset.Now;
                        log.Information($"Market start time is {MarketInfo.StartTime} on {DateTimeOffset.Now}, waiting until start time today: {ts}");
                        await Task.Delay(ts);
                        return false;
                    }
                    else
                    {
                        var ts = startTimeToday.AddDays(1) - DateTimeOffset.Now;
                        log.Information($"Market start time is {MarketInfo.StartTime} on {DateTimeOffset.Now}, waiting until start time tomorrow: {ts}");
                        await Task.Delay(ts);
                        return false;
                    }
                }
            }

            if (DateTimeOffset.Now < MarketInfo.StartTime)
            {
                var ts = MarketInfo.StartTime - DateTimeOffset.Now;
                log.Information($"Waiting for market open: {MarketInfo.StartTime}, {ts}");
                await Task.Delay(ts, token);
                return true;
            }
            else if (DateTimeOffset.Now > MarketInfo.EndTime)
            {
                var ts = MarketInfo.StartTime.AddDays(1) - DateTimeOffset.Now;
                log.Information($"Waiting until tomorrow, {ts}");
                await Task.Delay(ts, token);
                return false;
            }

            return true;
        }

        public async Task<List<PollResult>> PollWithRetry(int maxRetries = 10)
        {
            var backoff = new ExponentialBackoff(maxRetries, delayMilliseconds: 200, maxDelayMilliseconds: 120000);

        retry:
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += delegate { cts.Cancel(); };
            while (!await MarketOpenDelay(cts.Token)) ;
            var awaitable = Poll(cts.Token);
            try
            {
                var result = await awaitable.TimeoutAfter(TimeSpan.FromSeconds(60));
                return result;
            }
            catch (Exception e)
            {
                if (e is ApiException)
                {
                    log.Error(e, "API issue - backing off to retry in {delay} ms.", backoff.NextDelay);
                    await backoff.Delay();
                    goto retry;
                }
                else if (e is TimeoutException)
                {
                    cts.Cancel(); // signal cancellation -- mainly so we don't mess with stored tokens out-of-band

                    // Continue tracking it on the thread pool but abandon the results
                    _ = awaitable.ContinueWith((t) =>
                    {
                        log.Warning(t.Exception, "Information from task that was abandoned due to timeout: Status {status} Result {result}", t.Status, t.Result);
                    });

                    log.Error(e, "Timed out - backing off to retry in {delay} ms.", backoff.NextDelay);
                    await backoff.Delay();
                    goto retry;
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                cts.Dispose();
            }
        }
    }
}
