using Serilog;
namespace PollQT
{
    internal struct Context
    {
        public ILogger Logger { get; }
        public string WorkDir { get; }
        public Context(ILogger logger, string workDir) {
            Logger = logger;
            WorkDir = workDir;
        }
    }
}
