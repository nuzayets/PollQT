using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PollQT.DataTypes;
using Serilog;
namespace PollQT.OutputSinks
{
    internal class JsonLinesFileOutputSink : IOutputSink
    {
        private readonly ILogger log;
        private readonly string outDir;
        public JsonLinesFileOutputSink(Context context) {
            log = context.Logger.ForContext<JsonLinesFileOutputSink>();
            outDir = Path.Combine(context.WorkDir, "out");
            Directory.CreateDirectory(outDir);
        }
        public async Task NewEvent(List<PollResult> pollResults) {
            foreach (var pollResult in pollResults) {
                var outFile = Path.Combine(outDir, $"{pollResult.Timestamp:yyyyMMdd}.jsonl");
                log.Information("Writing account {AccountType}-{AccountNumber} {timestamp} results to {outFile}",
                    pollResult.Account.Type, pollResult.Account.Number, pollResult.Timestamp, outFile);
                await File.AppendAllLinesAsync(outFile, new string[] { pollResult.ToJson() });
            }
        }
    }
}
