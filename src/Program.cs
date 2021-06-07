using PollQT.DataTypes;
using PollQT.OutputSinks;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.IO;
using System.Threading;

namespace PollQT
{
    internal class Program
    {
        public static event EventHandler<PollResult>? RaisePollResults;

        private static void Main()
        {
            var workDir = Environment.GetEnvironmentVariable("POLLQT_WORKDIR") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pollqt");

            using var log = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File(Path.Combine(workDir, "logs/pollqt.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var context = new Context(log, workDir);
            var client = new Questrade.Client(context);

            var fileWriter = new JsonLinesFileOutputSink(context);
            RaisePollResults += async (s, e) => await fileWriter.NewEvent(e);

            while (true)
            {
                var pollResults = client.PollWithRetry().Result;
                log.Debug("Got poll result: {@res}", pollResults);
                foreach (var pollResult in pollResults)
                {
                    RaisePollResults(null, pollResult);
                }
                Thread.Sleep(60 * 1000);
            }



        }
    }
}
