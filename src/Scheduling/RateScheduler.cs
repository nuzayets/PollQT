using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace PollQT.Scheduling
{
    /// <summary>
    /// Allows events through at a rate of numerator / denominator e.g. 30 per second
    /// </summary>
    internal class RateScheduler : IScheduler, IDisposable
    {
        private readonly ILogger log;

        private readonly ManualResetEvent eventFuture;
        private readonly AutoResetEvent eventRate;
        private readonly Timer releaseTimer;
        private readonly TimeSpan oneEvery;
        private bool disposedValue;

        public RateScheduler(Context context, int numAllowed, TimeSpan perTimeSpan) {
            log = context.Logger.ForContext<RateScheduler>();
            eventRate = new AutoResetEvent(true);
            eventFuture = new ManualResetEvent(true);
            oneEvery = perTimeSpan.Divide(numAllowed);
            releaseTimer = new Timer((_) => {
                eventRate.Set();
            }, null, oneEvery, oneEvery);
        }

        public void RequestDelayUntil(DateTimeOffset future) {
            if (future > DateTimeOffset.Now) {
                eventFuture.Reset();
                Task.Delay(future - DateTimeOffset.Now).ContinueWith((_) => eventFuture.Set());
            }
        }

        public void WaitUntilRunnable() {
            var stopwatch = Stopwatch.StartNew();
            eventFuture.WaitOne();
            eventRate.WaitOne();
            stopwatch.Stop();
            log.Debug("Rate limit delay: {0}ms", stopwatch.ElapsedMilliseconds);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // these are backed by Win32 objects
                    // although this is probably unnecessary
                    eventFuture.Dispose();
                    eventRate.Dispose();
                    releaseTimer.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
