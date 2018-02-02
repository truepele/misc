using System;
using System.Threading.Tasks;
using truepele.Common;

namespace truepele
{
    public class SemaphoreSlim : System.Threading.SemaphoreSlim
    {
        public SemaphoreSlim(int initialCount) : base(initialCount)
        {
        }


        public SemaphoreSlim(int initialCount, int maxCount) : base(initialCount, maxCount)
        {
        }


        /// <summary>
        ///     Blocks current thread until it can enter the SemaphoreSlim.
        /// </summary>
        /// <returns>IDisposable which allows releasing the semaphor</returns>
        public IDisposable Enter()
        {
            Wait();
            return new AnonymousDisposable(() => Release());
        }


        /// <summary>
        ///     Blocks current thread until it can enter the SemaphoreSlim, using a 32-bit signed integer that specifies the
        ///     timeout.
        /// </summary>
        /// <returns>IDisposable which allows releasing the semaphor</returns>
        public IDisposable Enter(int millisecondsTimeout)
        {
            Wait(millisecondsTimeout);
            return new AnonymousDisposable(() => Release());
        }


        /// <summary>
        ///     Asynchronously waits to enter the SemaphoreSlim.
        /// </summary>
        /// <returns>IDisposable which allows releasing the semaphor</returns>
        public async Task<IDisposable> EnterAsync()
        {
            await WaitAsync().ConfigureAwait(false);
            return new AnonymousDisposable(() => Release());
        }


        /// <summary>
        ///     Asynchronously waits to enter the SemaphoreSlim, using a 32-bit signed integer to measure the time interval.
        /// </summary>
        /// <returns>IDisposable which allows releasing the semaphor</returns>
        public async Task<IDisposable> EnterAsync(int millisecondsTimeout)
        {
            await WaitAsync(millisecondsTimeout).ConfigureAwait(false);
            return new AnonymousDisposable(() => Release());
        }
    }
}