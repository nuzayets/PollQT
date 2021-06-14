using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PollQT.DataTypes;
using Serilog;
namespace PollQT.OutputSinks
{
    internal class InfluxLineProtolOutputSink : AbstractDeduplicatingOutputSink
    {
        protected override ILogger Log { get; }

        public InfluxLineProtolOutputSink(Context context) {
            Log = context.Logger.ForContext<InfluxLineProtolOutputSink>();
            Log.Information("Starting Influx Line Protocol output sink");
        }

        // See https://docs.influxdata.com/influxdb/v1.8/write_protocols/line_protocol_tutorial/#special-characters-and-keywords
        private static string EscapeGeneral(object v) => Regex.Replace(v.ToString() ?? "", "[,= ]", (m) => $"\\{m}");
        private static string EscapeMeasurement(object v) => Regex.Replace(v.ToString() ?? "", "[, ]", (m) => $"\\{m}");
        private static string Measurement(string m) => EscapeMeasurement(m);
        private static string Tag(string k, string v) => $",{EscapeGeneral(k)}={EscapeGeneral(v)}";

        private static string Field(string k, string v, bool first = false) => (first ? " " : ",") + $"{EscapeGeneral(k)}=\"{v}\"";
        private static string Field<T>(string k, T v, bool first = false) =>
            (v is string) ? Field(k, v.ToString() ?? "", first)
            : ((first ? " " : ",") + $"{EscapeGeneral(k)}={v}");
        private static string Time(DateTimeOffset t) => $" {((ulong)t.ToUnixTimeMilliseconds()) * 1000000}";

        private static void WritePositions(PollResult pollResult) {
            foreach (var position in pollResult.Positions) {
                var line = new StringBuilder()
                    .Append(Measurement("position"))
                    .Append(Tag("account", $"{pollResult.Account.Type}-{pollResult.Account.Number}"))
                    .Append(Tag("symbol", position.Symbol))
                    .Append(Field("open_quantity", position.OpenQuantity, first: true))
                    .Append(Field("current_market_value", position.CurrentMarketValue))
                    .Append(Field("current_price", position.CurrentPrice))
                    .Append(Field("total_cost", position.TotalCost))
                    .Append(Time(pollResult.Timestamp))
                    .ToString();
                Console.Out.WriteLine(line);
            }
        }
        private static void WriteBalances(PollResult pollResult) {
            if (pollResult.Balance.Currency.Trim().Length == 0) {
                return; // a balance without a currency is a placeholder
            }

            var line = new StringBuilder()
                .Append(Measurement("balance"))
                .Append(Tag("account", $"{pollResult.Account.Type}-{pollResult.Account.Number}"))
                .Append(Tag("currency", pollResult.Balance.Currency))
                .Append(Field("value", pollResult.Balance.TotalEquity, first: true))
                .Append(Field("cash", pollResult.Balance.Cash))
                .Append(Field("market_value", pollResult.Balance.MarketValue))
                .Append(Time(pollResult.Timestamp))
                .ToString();
            Console.Out.WriteLine(line);
        }

        private static void WriteActivities(PollResult pollResult) {
            foreach (var activity in pollResult.Activities) {
                var line = new StringBuilder()
                    .Append(Measurement("activity"))
                    .Append(Tag("account", $"{pollResult.Account.Type}-{pollResult.Account.Number}"))
                    .Append(Tag("type", activity.Type))
                    .Append((activity.Symbol.Trim().Length > 0) ? Tag("symbol", activity.Symbol) : "")
                    .Append(Field("net", activity.NetAmount, first: true))
                    .Append(Field("commission", activity.Commission))
                    .Append(Field("gross", activity.GrossAmount))
                    .Append(Field("price", activity.Price))
                    .Append(Field("quantity", activity.Quantity))
                    .Append(Time(activity.TransactionDate))
                    .ToString();
                Console.Out.WriteLine(line);
            }

        }

        protected override async Task Write(PollResult pollResult) {
            WriteBalances(pollResult);
            WritePositions(pollResult);
            WriteActivities(pollResult);
            await Console.Out.FlushAsync();
        }
    }
}
