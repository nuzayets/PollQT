using System;
using System.Threading.Tasks;

namespace PollQT.Questrade
{
    internal struct ExponentialBackoff
    {
        private readonly int maxRetries, delayMilliseconds, maxDelayMilliseconds;
        private int retries, pow;

        public int NextDelay => Math.Min(delayMilliseconds * (pow - 1) / 2, maxDelayMilliseconds);

        public ExponentialBackoff(int maxRetries, int delayMilliseconds,
            int maxDelayMilliseconds) {
            this.maxRetries = maxRetries;
            this.delayMilliseconds = delayMilliseconds;
            this.maxDelayMilliseconds = maxDelayMilliseconds;
            retries = 0;
            pow = 1;
        }

        public Task Delay() {
            if (retries == maxRetries) {
                throw new TimeoutException("Max retry attempts exceeded.");
            }
            ++retries;
            if (retries < 31) {
                pow <<= 1; // m_pow = Pow(2, m_retries - 1)
            }
            var delay = NextDelay;
            return Task.Delay(delay);
        }
    }
}
