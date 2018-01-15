using System;
using System.Threading;
using System.Threading.Tasks;

namespace truepele
{
    // TODO: 
    // - Extract factory retrier ?
    // - implement IDisposable
    // - extract semaphore Wait/Release logic

    public class UpdatableLazy<T>
    {
        private readonly Func<T, T> _factory;
        private readonly int _retryCount;
        private readonly SemaphoreSlim _semaphor = new SemaphoreSlim(1, 1);
        private bool _updateIsInProgress;
        private T _value;

        public UpdatableLazy(Func<T, T> valueFactory, int retryCount = 1)
        {
            _factory = valueFactory;
            _retryCount = retryCount;
        }

        public UpdatableLazy(Func<T> valueFactory, int retryCount = 1)
            : this(value  => valueFactory(), retryCount)
        {
        }


        public T Value
        {
            get
            {
                _semaphor.Wait();

                try
                {
                    if (_value != null)
                    {
                        return _value;
                    }
                }
                finally
                {
                    _semaphor.Release();
                }

                return UpdateOrWait();
            }
        }

        public event Action<T> ValueCreated;


        public async Task<T> GetValueAsync()
        {
            await _semaphor.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_value != null)
                {
                    return _value;
                }
            }
            finally
            {
                _semaphor.Release();
            }

            return await UpdateOrWaitAsync();
        }


        public async Task<T> UpdateOrWaitAsync()
        {
            var wasInprogress = _updateIsInProgress;
            await _semaphor.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!wasInprogress)
                {
                    await Task.Run(() => LoadValueWithRetry());
                }

                return _value;
            }
            finally
            {
                _semaphor.Release();
            }
        }


        public T UpdateOrWait()
        {
            var wasInprogress = _updateIsInProgress;
            _semaphor.Wait();

            try
            {
                if (!wasInprogress)
                {
                    LoadValueWithRetry();
                }

                return _value;
            }
            finally
            {
                _semaphor.Release();
            }
        }


        private void LoadValueWithRetry()
        {
            _updateIsInProgress = true;

            try
            {
                for (var i = 0; i < _retryCount; i++)
                {
                    try
                    {
                        _value = _factory(_value);
                    }
                    catch (Exception)
                    {
                        // TODO: Error event
                        continue;
                    }

                    ValueCreated?.Invoke(_value);
                    return;
                }

                // TODO: Max attempts reached event
            }
            finally
            {
                _updateIsInProgress = false;
            }
        }
    }
}