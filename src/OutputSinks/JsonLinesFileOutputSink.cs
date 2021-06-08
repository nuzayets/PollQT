using System.IO;
using System.Threading.Tasks;
using PollQT.DataTypes;
using Serilog;
namespace PollQT.OutputSinks
{
    internal class JsonLinesFileOutputSink : AbstractDeduplicatingOutputSink
    {
        protected override ILogger Log { get; }

        private readonly string outDir;
        public JsonLinesFileOutputSink(Context context) {
            Log = context.Logger.ForContext<JsonLinesFileOutputSink>();
            outDir = Path.Combine(context.WorkDir, "out");
            Directory.CreateDirectory(outDir);
        }

        protected override async Task Write(PollResult pollResult) {
            var outFile = Path.Combine(outDir, $"{pollResult.Timestamp:yyyyMMdd}.jsonl");
            Log.Information("Writing account {AccountType}-{AccountNumber} {timestamp} results to {outFile}",
                pollResult.Account.Type, pollResult.Account.Number, pollResult.Timestamp, outFile);
            await File.AppendAllLinesAsync(outFile, new string[] { pollResult.ToJson() });
        }
    }
}
