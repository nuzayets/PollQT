using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private static Questrade.Client? client;
        private static ILogger? log;

        /// <param name="workDir">The directory to store token file, logs, and output</param>
        /// <param name="logLevel">Minimum Log Level, one of Verbose, Debug, Information, Warning, Error</param>
        /// <param name="influxOutput">True to output Influx Line Protocol on STDOUT (for use with Telegraf)</param>
        /// <param name="fileOutput">True to output JSONL to <c>workDir/out/yyyyMMdd.jsonl</c></param>
        /// <param name="logConsole">True to log to console</param>
        /// <param name="logFile">True to log to file in <c>workDir/log/pollqtyyyyMMdd.log</c></param>
        /// <param name="convertFile">Convert a JSONL file to Influx Line Protocol and exit</param>
        private static async Task Main(
            string workDir,
            string logLevel = "Information",
            bool influxOutput = true,
            bool fileOutput = false,
            bool logConsole = false,
            bool logFile = true,
            string convertFile = "") {

            if (convertFile.Length > 0) {
                await DoConvertFile(convertFile);
                return;
            }

            var workDirFinal = workDir.Length > 0 ? workDir : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pollqt");
            var logConfig = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .MinimumLevel.ControlledBy(new ArgumentLoggingLevelSwitch(logLevel));
            if (logConsole) {
                logConfig = logConfig
                    .WriteTo.Console(
                    // if we're building in debug mode, we're not actually running under influx so just do the default
#if !DEBUG
                    // write warnings and above only if we are in influx mode (Telegraf plugin errors mean something)
                    restrictedToMinimumLevel: influxOutput ? Serilog.Events.LogEventLevel.Warning : Serilog.Events.LogEventLevel.Verbose,
                    // write everything to stderr in influx mode (because stdout is our output), otherwise everything to stdout
                    standardErrorFromLevel: influxOutput ? Serilog.Events.LogEventLevel.Verbose : null,
#endif
                    theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {SourceContext}-{ThreadId} {Level:u3}] {Message:l}{NewLine}{Exception}");
            }
            if (logFile) {
                logConfig = logConfig
                    .WriteTo.File(
                        Path.Combine(workDirFinal, "logs/pollqt.log"),
                        outputTemplate: "[{Timestamp:HH:mm:ss} {SourceContext}-{ThreadId} {Level:u3}] {Message:l}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day);
            }
            log = logConfig.CreateLogger();
            var context = new Context(log, workDirFinal);
            client = new Questrade.Client(context);
            if (fileOutput) {
                var fileWriter = new JsonLinesFileOutputSink(context);
                RaisePollResults += async (s, e) => await fileWriter.NewEvent(e);
            }
            if (influxOutput) {
                var influxWriter = new InfluxLineProtolOutputSink(context);
                RaisePollResults += async (s, e) => await influxWriter.NewEvent(e);
            }
            await DoPollIndefinitely();
        }

        private static async Task DoPollIndefinitely() {
            while (true) {
                try {
                    if (client != null) {
                        var pollResults = await client.PollWithRetry();
                        log?.Verbose("Got poll result: {@res}", pollResults);
                        RaisePollResults?.Invoke(null, pollResults);
                    } else {
                        log?.Error("Static client is null. This is weird.");
                    }
                } catch (Exception e) {
                    log?.Error(e, "Unhandled exception");
                }
                Thread.Sleep(60 * 1000);
            }
        }

        private static async Task DoConvertFile(string convertFile) {
            var influxWriter = new InfluxLineProtolOutputSink(new Context(Log.Logger, ""));
            var results = new List<PollResult>(File.ReadAllLines(convertFile).Select(l => PollResult.FromJson(l)));
            await influxWriter.NewEvent(results);
        }
    }
}
