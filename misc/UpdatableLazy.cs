using System;
using System.Collections.Generic;
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
        private readonly int _maxRetries;
        private readonly SemaphoreSlim _semaphor = new SemaphoreSlim(1, 1);
        private bool _updateIsInProgress;
        private T _value;

        public UpdatableLazy(Func<T, T> valueFactory, int maxRetries = 3)
        {
            _factory = valueFactory;
            _maxRetries = maxRetries;
        }

        public UpdatableLazy(Func<T> valueFactory, int maxRetries = 3)
            : this(value => valueFactory(), maxRetries)
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

        public event Action<object, T> ValueCreated;

        public event Action<object, Exception> FactoryError;

        public event Action<object, IEnumerable<Exception>> MaxRetriesExceeded;


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
            var exceptions = new List<Exception>();

            try
            {
                for (var i = 0; i <= _maxRetries; i++)
                {
                    try
                    {
                        _value = _factory(_value);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        Task.Run(() => FactoryError?.Invoke(this, e));
                        continue;
                    }

                    Task.Run(() => ValueCreated?.Invoke(this, _value));
                    return;
                }

                Task.Run(() => MaxRetriesExceeded?.Invoke(this, exceptions));
            }
            finally
            {
                _updateIsInProgress = false;
            }
        }
    }
}