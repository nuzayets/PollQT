using PollQT.DataTypes;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace PollQT.OutputSinks
{
    internal class JsonLinesFileOutputSink : IOutputSink
    {
        private readonly ILogger log;
        private readonly string outDir;

        public JsonLinesFileOutputSink(Context context)
        {
            log = context.Logger;
            outDir = Path.Combine(context.WorkDir, "out");
            Directory.CreateDirectory(outDir);
        }

        public async Task NewEvent(PollResult pollResults)
        {
            var outFile = Path.Combine(outDir, $"{pollResults.Timestamp:yyyyMMdd}.jsonl");
            log.Information("Writing {timestamp} results to {outFile}", pollResults.Timestamp, outFile);
            await File.AppendAllLinesAsync(outFile, new string[] { pollResults.ToJson() });
        }
    }
}
