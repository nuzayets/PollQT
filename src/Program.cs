using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PollQT.DataTypes;
using PollQT.OutputSinks;
using PollQT.Util;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace PollQT
{
    internal class Program
    {
        public static event EventHandler<List<PollResult>>? RaisePollResults;

        /// <param name="workDir">The directory to store token file, logs, and output.</param>
        /// <param name="logLevel">Minimum Log Level, one of Verbose, Debug, Information, Warning, Error.</param>
        /// <param name="influxOutput">True to output Influx Line Protocol on STDOUT (for use with Telegraf)</param>
        /// <param name="fileOutput">True to output JSONL to <c>workDir/out/yyyyMMdd.jsonl</c></param>
        /// <param name="logConsole">True to log to console</param>
        /// <param name="logFile">True to log to file in <c>workDir/log/pollqtyyyyMMdd.log</c></param>
        private static void Main(
            string workDir,
            string logLevel = "Information",
            bool influxOutput = true,
            bool fileOutput = false,
            bool logConsole = false,
            bool logFile = true) {

            var workDirFinal = workDir.Length > 0 ? workDir : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pollqt");
            var logConfig = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .Enrich.WithUtcTimestamp()
                .MinimumLevel.ControlledBy(new ArgumentLoggingLevelSwitch(logLevel));

            if (logConsole) {
                if (influxOutput) {
                    Console.Error.WriteLine("WARNING: Logging to console defeats the purpose of getting Influx Line Protocol on STDOUT!");
                }
                logConfig = logConfig
                    .WriteTo.Console(theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{UtcTimestamp:HH:mm:ssK} {SourceContext}-{ThreadId} {Level:u3}] {Message:l}{NewLine}{Exception}",
                    standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose);
            }

            if (logFile) {
                logConfig = logConfig
                    .WriteTo.Map("UtcTimestamp", DateTime.UtcNow,
                    (UtcDateTime, wt) => wt.File(
                        Path.Combine(workDirFinal, $"logs/pollqt{UtcDateTime:yyyyMMdd}.log"),
                        outputTemplate: "[{UtcTimestamp:HH:mm:ssK} {SourceContext}-{ThreadId} {Level:u3}] {Message:l}{NewLine}{Exception}"));
            }

            using var log = logConfig.CreateLogger();

            var context = new Context(log, workDirFinal);
            var client = new Questrade.Client(context);

            if (fileOutput) {
                var fileWriter = new JsonLinesFileOutputSink(context);
                RaisePollResults += async (s, e) => await fileWriter.NewEvent(e);
            }

            if (influxOutput) {
                var influxWriter = new InfluxLineProtolOutputSink(context);
                RaisePollResults += async (s, e) => await influxWriter.NewEvent(e);
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
