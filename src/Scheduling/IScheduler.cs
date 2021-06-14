using System;

namespace PollQT.Scheduling
{
    internal interface ISchedulerToken : IDisposable { };

    /// <summary>
    /// A scheduler here acts more like a gate, allowing things through only at the appropriate times.
    /// </summary>
    internal interface IScheduler
    {
        /// <summary>
        /// Blocks the calling thread until it is allowed to run based on the configuration of the scheduler
        /// </summary>
        public void WaitUntilRunnable();

        public void RequestDelayUntil(DateTimeOffset future);
    }
}
