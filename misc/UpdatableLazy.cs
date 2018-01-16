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
    // - Fluent interface
    // - Concurent Value Container for _updateIsInProgress

    public class UpdatableLazy<T>
    {
        private readonly Func<T, Task<T>> _factory;
        private readonly int _maxRetries;
        private readonly SemaphoreSlim _semaphor = new SemaphoreSlim(1, 1);
        private bool _updateIsInProgress;
        private T _value;

        public UpdatableLazy(Func<T, Task<T>> valueFactory, int maxRetries = 3)
        {
            _factory = valueFactory;
            _maxRetries = maxRetries;
        }


        public UpdatableLazy(Func<Task<T>> valueFactory, int maxRetries = 3)
            : this(value => valueFactory(), maxRetries) { }


        public UpdatableLazy(Func<T, T> valueFactory, int maxRetries = 3)
            : this(async v => valueFactory(v), maxRetries)
        {
        }


        public UpdatableLazy(Func<T> valueFactory, int maxRetries = 3)
            : this(value => valueFactory(), maxRetries) {}


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

            return await UpdateOrWaitAsync().ConfigureAwait(false);
        }


        public async Task<T> UpdateOrWaitAsync()
        {
            var wasInprogress = _updateIsInProgress;
            await _semaphor.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!wasInprogress)
                {
                    await LoadValueWithRetryAsync().ConfigureAwait(false);
                }

                return _value;
            }
            finally
            {
                _semaphor.Release();
            }
        }

        public void StartUpdateIfNotInProgress()
        {
            Task.Run(UpdateOrWaitAsync);
        }


        public T UpdateOrWait()
        {
            var wasInprogress = _updateIsInProgress;
            _semaphor.Wait();

            try
            {
                if (!wasInprogress)
                {
                    LoadValueWithRetryAsync().Wait();
                }

                return _value;
            }
            finally
            {
                _semaphor.Release();
            }
        }


        private async Task LoadValueWithRetryAsync()
        {
            _updateIsInProgress = true;
            var exceptions = new List<Exception>();

            try
            {
                for (var i = 0; i <= _maxRetries; i++)
                {
                    try
                    {
                        _value = await _factory(_value).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        Task.Run(() => FactoryError?.Invoke(this, e));
                        continue;
                    }

                    // TODO: Check for null before running a task
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