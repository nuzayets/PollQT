using System;
using System.Threading;

namespace PollQT.Scheduling
{
    /// <summary>
    /// Allows events through at a rate of numerator / denominator e.g. 30 per second
    /// </summary>
    class RateScheduler : IScheduler, IDisposable
    {
        private readonly Timer rateTimer;
        private readonly ManualResetEvent mreFuture;
        private readonly Semaphore semaphore;
        private bool disposedValue;

        public RateScheduler(int numerator, TimeSpan denominator) {
            semaphore = new Semaphore(numerator, numerator);
            rateTimer = new Timer((_) => { 
                try { 
                    semaphore.Release(numerator);
                } catch (SemaphoreFullException) {
                    
                }

            }, null, denominator, denominator);
            mreFuture = new ManualResetEvent(true);
        }

        public void RequestDelayUntil(DateTimeOffset future) {
            if (future > DateTimeOffset.Now) {
                mreFuture.Reset();
                var timer = new Timer((_) => mreFuture.Set(), null, DateTimeOffset.Now - future, Timeout.InfiniteTimeSpan);
            }
        }

        public void WaitUntilRunnable() => _ = mreFuture.WaitOne() && semaphore.WaitOne();

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // these are backed by Win32 objects
                    // although this is probably unnecessary
                    rateTimer.Dispose();
                    semaphore.Dispose();
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
