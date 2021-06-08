using System;
using Serilog.Core;
using Serilog.Events;

namespace PollQT.Util
{
    internal class ArgumentLoggingLevelSwitch : LoggingLevelSwitch
    {
        public ArgumentLoggingLevelSwitch(string arg) {
            if (Enum.TryParse<LogEventLevel>(arg, true, out var level)) {
                MinimumLevel = level;
            }
        }
    }
}
