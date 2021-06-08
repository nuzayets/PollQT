using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using PollQT.DataTypes;
using Serilog;

namespace PollQT.OutputSinks
{
    class InfluxLineProtolOutputSink : IOutputSink
    {
        private readonly ILogger log;
        private readonly ConcurrentDictionary<string, PollResult> prevResults = new ConcurrentDictionary<string, PollResult>();

        public InfluxLineProtolOutputSink(Context context) {
            log = context.Logger.ForContext<InfluxLineProtolOutputSink>();
            log.Information("Starting Influx Line Protocol output sink");
        }


        private bool ShouldWrite(PollResult pollResult) {
            // don't write entries with the exact same data as before
            var entry = prevResults.GetOrAdd(pollResult.Account.Number, pollResult);

            if (pollResult.Positions.SequenceEqual(entry.Positions)
                && pollResult.Balance.Equals(entry.Balance)) {
                // if the data match & timestamp match, do write - GetOrAdd did Add
                // if the data match & timestamp don't match, don't write - GetOrAdd did Get of the same
                return pollResult.Timestamp == entry.Timestamp;
            } else {
                // always write if the data don't match
                _ = prevResults.TryUpdate(pollResult.Account.Number, pollResult, entry);
                return true;
            }
        }

        public async Task NewEvent(List<PollResult> pollResults) {
            foreach (var pollResult in pollResults.Where(ShouldWrite)) {
                WriteBalances(pollResult);
                WritePositions(pollResult);
            }
            await Task.CompletedTask;
        }

        private string Measurement(string m) => m;
        private string Tag(string k, string v) => $",{k}={v}";
        private string Value<T>(string k, T v, bool first = false) => first ? $" {k}={v}" : $",{k}={v}";
        private string Time(DateTimeOffset t) => $" {(((UInt64)t.ToUnixTimeMilliseconds()) * 1000000).ToString()}";

        private void WritePositions(PollResult pollResult) {
            foreach (var position in pollResult.Positions) {
                var line = new StringBuilder()
                    .Append(Measurement("position"))
                    .Append(Tag("account", $"{pollResult.Account.Type}-{pollResult.Account.Number}"))
                    .Append(Tag("symbol", position.Symbol))
                    .Append(Value("open_quantity", position.OpenQuantity, first: true))
                    .Append(Value("current_market_value", position.CurrentMarketValue))
                    .Append(Value("current_price", position.CurrentPrice))
                    .Append(Value("total_cost", position.TotalCost))
                    .Append(Time(pollResult.Timestamp))
                    .ToString();
                Console.Out.WriteLine(line);
            }
        }
        private void WriteBalances(PollResult pollResult) {
            var line = new StringBuilder()
                .Append(Measurement("balance"))
                .Append(Tag("account", $"{pollResult.Account.Type}-{pollResult.Account.Number}"))
                .Append(Tag("currency", pollResult.Balance.Currency))
                .Append(Value("value", pollResult.Balance.TotalEquity, first: true))
                .Append(Value("cash", pollResult.Balance.Cash))
                .Append(Value("market_value", pollResult.Balance.MarketValue))
                .Append(Time(pollResult.Timestamp))
                .ToString();
            Console.Out.WriteLine(line);
        }
    }
}
