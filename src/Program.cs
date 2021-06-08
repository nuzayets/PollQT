using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PollQT.DataTypes;
using PollQT.OutputSinks;
using PollQT.Util;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace PollQT
{
    internal class Program
    {
        public static event EventHandler<List<PollResult>>? RaisePollResults;

        private static string ConfigureWorkingDirectory() {
#if DEBUG
            var workDirName = ".pollqt-dbg";
#else
            var workDirName = ".pollqt";
#endif

            var workDir = Environment.GetEnvironmentVariable("POLLQT_WORKDIR") ??
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), workDirName);

            return workDir;
        }

        private static Logger ConfigureLogger(string workDir) {
#if DEBUG
            var baseConfig = new LoggerConfiguration().MinimumLevel.Verbose();
#else
            var baseConfig = new LoggerConfiguration().MinimumLevel.Information();
#endif
            return baseConfig
                .Enrich.WithThreadId()
                .Enrich.WithUtcTimestamp()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{UtcTimestamp:HH:mm:ssK} {SourceContext}-{ThreadId} {Level:u3}] {Message:l}{NewLine}{Exception}",
                    standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.Map("UtcTimestamp", DateTime.UtcNow,
                    (UtcDateTime, wt) => wt.File(Path.Combine(workDir, $"logs/pollqt{UtcDateTime:yyyyMMdd}.log")))
                .CreateLogger();
        }

        private static void Main() {
            var workDir = ConfigureWorkingDirectory();
            using var log = ConfigureLogger(workDir);

            var context = new Context(log, workDir);
            var client = new Questrade.Client(context);

            if (Environment.GetEnvironmentVariable("FILE_OUT") != null) {
                var fileWriter = new JsonLinesFileOutputSink(context);
                RaisePollResults += async (s, e) => await fileWriter.NewEvent(e);
            }
            
            if (Environment.GetEnvironmentVariable("TS_DBNAME") != null) { 
                using var awsWriter = new AwsTimestreamOutputSink(context);
                RaisePollResults += async (s, e) => await awsWriter.NewEvent(e);
            }

            if (Environment.GetEnvironmentVariable("INFLUX_OUT") != null || true) {
                var fileWriter = new InfluxLineProtolOutputSink(context);
                RaisePollResults += async (s, e) => await fileWriter.NewEvent(e);
            }

            while (true) {
                try { 
                    var pollResults = client.PollWithRetry().Result;
                    log.Verbose("Got poll result: {@res}", pollResults);
                    RaisePollResults?.Invoke(null, pollResults);
                } catch (Exception e) {
                    log.Error(e, "Unhandled exception");
                }
                Thread.Sleep(60 * 1000);
            }
        }
    }
}
