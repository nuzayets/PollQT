using System;
using System.IO;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace PollQT
{
    class Program
    {

        static void Main()
        {
            var workDir = Environment.GetEnvironmentVariable("POLLQT_WORKDIR") ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".pollqt");

            using var log = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File(Path.Combine(workDir, "logs/pollqt.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var context = new Questrade.Context(log, workDir);

            var pollResult = new Questrade.Client(context).PollWithRetry().Result;

            log.Debug("Got poll result: {@res}", pollResult);
        }
    }
}
