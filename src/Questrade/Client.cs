using PollQT.Questrade.Responses;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PollQT.Questrade
{
    
    public class Client
    {
        private Token Token { get; set;  }
        private HttpClient HttpClient { get; set;  }

        private readonly ITokenStore tokenStore;
        private readonly ILogger log;

        private enum RequestType
        {
            TOKEN,
            ACCOUNTS,
            ACCOUNT_BALANCES,
            ACCOUNT_POSITIONS,
        }


        public Client(Context context)
        {
            this.log = context.Logger;
            HttpClient = new HttpClient();
            Token = new Token();

            log.Information("workDir: {workDir}", context.WorkDir);
            if (!Directory.Exists(context.WorkDir))
            {
                log.Information("Created {workDir}", context.WorkDir);
                Directory.CreateDirectory(context.WorkDir);
            }

            this.tokenStore = new FileTokenStore(log, Path.Combine(context.WorkDir, "token.json"));
            log.Information("tokenStore: {tokenStore}", tokenStore);

            NewToken(tokenStore.GetToken(), supressWrite: true);   
        }

        private string RequestRoute(RequestType requestType, string? id = default)
        {
            return requestType switch
            {
                RequestType.TOKEN => 
                    $"https://login.questrade.com/oauth2/token?grant_type=refresh_token&refresh_token={Token.RefreshToken}",

                RequestType.ACCOUNTS => "/v1/accounts/",

                RequestType.ACCOUNT_BALANCES => $"/v1/accounts/{id}/balances",

                RequestType.ACCOUNT_POSITIONS => $"/v1/accounts/{id}/positions",

                _ => throw new NotImplementedException()
            };
        }

        private void NewToken(Token token, bool supressWrite = false)
        {
            Token = token;
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token.AccessToken}");
            if (token.ApiServer != null) HttpClient.BaseAddress = new Uri(token.ApiServer);
            if (!supressWrite)
            {
                tokenStore.WriteToken(token);
            }
        }

        private class ApiException : Exception { 
            public ApiException(string message): base(message) {}
            public ApiException() : base() { }
        }
        private class UnauthorizedException : ApiException { }
        private class UnexpectedStatusException : ApiException { }

        private async Task<string> MakeRequest(RequestType type, string? id = default)
        {
            string route = RequestRoute(type, id);
            log.Information("Request: {type}", type);
            var stopWatch = Stopwatch.StartNew();
            var resp = await HttpClient.GetAsync(route);
            stopWatch.Stop();
            log.Debug("Request: {request}", resp.RequestMessage);
            log.Debug("Response: {resp}", resp);
            if (!resp.IsSuccessStatusCode)
            {
                log.Error("Error: {type} {status} ({dur}ms)", type, resp.StatusCode, stopWatch.ElapsedMilliseconds);
                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // invalidate token but keep refresh; will login on next attempt
                    NewToken(new Token { RefreshToken = Token.RefreshToken });
                    throw new UnauthorizedException();
                }
                else
                {
                    throw new UnexpectedStatusException();
                }
            }

            log.Information("Success: {type} ({dur}ms)", type, stopWatch.ElapsedMilliseconds);
            return await resp.Content.ReadAsStringAsync();
        }

        private async Task Login()
        {
            log.Information("Using refresh token to login.");
            var token = await MakeRequest(RequestType.TOKEN);
            NewToken(Token.FromJson(token));
            log.Information("Successfully got token.");
        }

        private async Task<T> GetResponse<T>(RequestType type, string? id = default) where T : JsonSerializable<T>
        {
            var res = await MakeRequest(type, id);
            var resObj = JsonSerializable<T>.FromJson(res);
            log.Information("{resName}: {@res}", resObj.GetType(), resObj);
            return resObj;
        }
        
        // wish I could use Record type, alas this may run in an AWS Lambda handler which currently supports NET Core 3.1
        // unless I build a custom image but who has the time for that
        private async Task<List<(Account Account, AccountBalance Balance, List<AccountPositions> Positions)>> Poll()
        {
            if (Token.ApiServer is null || Token.AccessToken is null)
            {
                await Login();
            }
            var resp = new List<(Account Account, AccountBalance Balance, List<AccountPositions> Positions)>();
            var accounts = await GetResponse<AccountsResponse>(RequestType.ACCOUNTS);
            foreach (var account in accounts.Accounts)
            {
                var accountBalances = await GetResponse<AccountBalancesResponse>(RequestType.ACCOUNT_BALANCES, account.Number);
                var accountPositions = await GetResponse<AccountPositionsResponse>(RequestType.ACCOUNT_POSITIONS, account.Number);
                resp.Add((account, accountBalances.CombinedBalances[0], accountPositions.Positions));
            }
            return resp;
        }

        public async Task<List<(Account Account, AccountBalance Balance, List<AccountPositions> Positions)>> PollWithRetry(int maxRetries = 10)
        {
            ExponentialBackoff backoff = new ExponentialBackoff(maxRetries, delayMilliseconds: 200, maxDelayMilliseconds: 120000);
            retry:
            try
            {
                return await Poll();
            } 
            catch (ApiException e)
            {
                Log.Error("API exception {e}, backing off to retry in {delay} ms.", e, backoff.NextDelay);
                await backoff.Delay();
                goto retry;
            }
        }
    }
}
