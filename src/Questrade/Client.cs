using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using PollQT.DataTypes;
using PollQT.Questrade.Responses;
using Serilog;
namespace PollQT.Questrade
{
    internal class Client
    {
        private Token Token { get; set; }
        private HttpClient HttpClient { get; set; }
        private readonly ITokenStore tokenStore;
        private readonly ILogger log;
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
            Token = new Token();
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
                $"oauth2/token?grant_type=refresh_token&refresh_token={Token.RefreshToken}",
            RequestType.ACCOUNTS => "v1/accounts",
            RequestType.ACCOUNT_BALANCES => $"v1/accounts/{id}/balances",
            RequestType.ACCOUNT_POSITIONS => $"v1/accounts/{id}/positions",
            _ => throw new NotImplementedException()
        };
        private string RequestBaseAddress(RequestType requestType) => requestType switch
        {
            RequestType.TOKEN => "https://login.questrade.com/",
            // if ApiServer isn't set, we need a new token, throw Unauthorized so we relogin
            _ => Token.ApiServer ?? throw new UnauthorizedException()
        };
        private void NewToken(Token token, bool supressWrite = false) {
            Token = token;
            HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token.AccessToken}");
            if (!supressWrite) {
                tokenStore.WriteToken(token);
            }
        }
        private class ApiException : Exception
        {
            public ApiException(string message) : base(message) { }
            public ApiException() : base() { }
        }
        private class UnauthorizedException : ApiException
        {
            public UnauthorizedException() : base() { }
            public UnauthorizedException(string message) : base(message) { }
        }
        private class UnexpectedStatusException : ApiException
        {
            public HttpResponseMessage HttpResponse { get; }
            public UnexpectedStatusException(HttpResponseMessage httpResponse) => HttpResponse = httpResponse;
        }
        private async Task<string> MakeRequest(RequestType type, string? id = default, int retries = 0) {
            var stopwatch = Stopwatch.StartNew();
            var route = RequestBaseAddress(type) + RequestRoute(type, id);
            var resp = await HttpClient.GetAsync(route);
            stopwatch.Stop();
            log.Debug("Request: {request}", resp.RequestMessage);
            log.Debug("Response: {resp}", resp);
            if (!resp.IsSuccessStatusCode) {
                if (resp.StatusCode == HttpStatusCode.Unauthorized && retries == 0) {
                    log.Information("Unuthorized: {type} ({dur}ms)", type, stopwatch.ElapsedMilliseconds);
                    // We get unauthorized often -- let's not throw an exception, log in here and retry
                    await Login();
                    return await MakeRequest(type, id, retries: retries + 1);
                } else {
                    log.Error("Error: {type} {resp} ({dur}ms)", type, resp, stopwatch.ElapsedMilliseconds);
                    throw resp.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => new UnauthorizedException("Remained unauthorized after retry."),
                        _ => new UnexpectedStatusException(resp),
                    };
                }
            }
            log.Information("Success: {type} ({dur}ms)", type, stopwatch.ElapsedMilliseconds);
            return await resp.Content.ReadAsStringAsync();
        }
        private async Task Login() {
            log.Information("Logging in with refresh token");
            var token = await MakeRequest(RequestType.TOKEN);
            log.Verbose("Body: {@res}", token);
            NewToken(Token.FromJson(token));
            log.Information("Successfully logged in with refresh token");
        }
        private async Task<T> GetResponse<T>(RequestType type, string? id = default) where T : JsonSerializable<T> {
            var res = await MakeRequest(type, id);
            log.Verbose("Body: {@res}", res);
            var resObj = JsonSerializable<T>.FromJson(res);
            log.Debug("{resName}: {@res}", resObj.GetType(), resObj);
            return resObj;
        }
        private async Task<List<PollResult>> Poll() {
            if (Token.ApiServer is null || Token.AccessToken is null) {
                await Login();
            }
            var resp = new List<PollResult>();
            var accounts = await GetResponse<AccountsResponse>(RequestType.ACCOUNTS);
            foreach (var account in accounts.Accounts) {
                var accountBalances = await GetResponse<AccountBalancesResponse>(RequestType.ACCOUNT_BALANCES, account.Number);
                var accountPositions = await GetResponse<AccountPositionsResponse>(RequestType.ACCOUNT_POSITIONS, account.Number);
                resp.Add(new PollResult(account, accountBalances.CombinedBalances[0], accountPositions.Positions));
            }
            return resp;
        }
        public async Task<List<PollResult>> PollWithRetry(int maxRetries = 10) {
            var backoff = new ExponentialBackoff(maxRetries, delayMilliseconds: 200, maxDelayMilliseconds: 120000);
        retry:
            try {
                return await Poll();
            } catch (ApiException e) {
                log.Error(e, "API error - backing off to retry in {delay} ms.", backoff.NextDelay);
                await backoff.Delay();
                goto retry;
            }
        }
    }
}
