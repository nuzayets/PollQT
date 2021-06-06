using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PollQT.Questrade
{
    public struct Context
    {
        public ILogger Logger { get; }
        public string WorkDir { get; }

        public Context(ILogger logger, string workDir)
        {
            this.Logger = logger;
            this.WorkDir = workDir;
        }
    }
}
