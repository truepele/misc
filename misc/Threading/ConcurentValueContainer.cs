using System;

namespace truepele.Threading
{
    public class ConcurentValueContainer<T> : IDisposable
    {
        private readonly SemaphoreSlim _semaphor = new SemaphoreSlim(1, 1);
        private T _value;

        public T Value
        {
            get
            {
                using (_semaphor.Enter())
                {
                    return _value;
                }
            }
            set
            {
                using (_semaphor.Enter())
                {
                    _value = value;
                }
            }
        }

        public void Dispose()
        {
            _semaphor.Dispose();
        }
    }
}