using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using truepele.Threading;

namespace truepele.Common
{
    public class UpdatableLazy<T> : IDisposable
    {
        private readonly Func<T, Task<T>> _factory;
        private readonly int _maxRetries;
        private readonly SemaphoreSlim _semaphor = new SemaphoreSlim(1, 1);
        private readonly ConcurentValueContainer<bool> _updateIsInProgress = new ConcurentValueContainer<bool>();
        private T _value;

        public UpdatableLazy(Func<T, Task<T>> valueFactory, int maxRetries = 3)
        {
            _factory = valueFactory;
            _maxRetries = maxRetries;
        }


        public UpdatableLazy(Func<Task<T>> valueFactory, int maxRetries = 3)
            : this(value => valueFactory(), maxRetries)
        {
        }


        public UpdatableLazy(Func<T, T> valueFactory, int maxRetries = 3)
            : this(async v => valueFactory(v), maxRetries)
        {
        }


        public UpdatableLazy(Func<T> valueFactory, int maxRetries = 3)
            : this(value => valueFactory(), maxRetries)
        {
        }
        

        public event EventHandler<T> ValueCreated;

        public event EventHandler<Exception> FactoryError;

        public event EventHandler<IEnumerable<Exception>> MaxRetriesExceeded;


        public T Value
        {
            get
            {
                using (_semaphor.Enter())
                {
                    if (_value != null)
                    {
                        return _value;
                    }
                }

                return UpdateOrWait();
            }
        }


        public void Dispose()
        {
            _semaphor.Dispose();
            _updateIsInProgress.Dispose();
        }


        public async Task<T> GetValueAsync()
        {
            using (await _semaphor.EnterAsync().ConfigureAwait(false))
            {
                if (_value != null)
                {
                    return _value;
                }
            }

            return await UpdateOrWaitAsync().ConfigureAwait(false);
        }


        public async Task<T> UpdateOrWaitAsync()
        {
            var wasInprogress = _updateIsInProgress.Value;

            using (await _semaphor.EnterAsync().ConfigureAwait(false))
            {
                if (!wasInprogress)
                {
                    await LoadValueWithRetryAsync().ConfigureAwait(false);
                }

                return _value;
            }
        }

        public void StartUpdateIfNotInProgress()
        {
            Task.Run(UpdateOrWaitAsync);
        }


        public T UpdateOrWait()
        {
            var wasInprogress = _updateIsInProgress.Value;

            using (_semaphor.Enter())
            {
                if (!wasInprogress)
                {
                    LoadValueWithRetryAsync().Wait();
                }

                return _value;
            }
        }


        private async Task LoadValueWithRetryAsync()
        {
            _updateIsInProgress.Value = true;
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
                        FactoryError?.BeginRaiseEvent(this, e);
                        continue;
                    }

                    ValueCreated?.BeginRaiseEvent(this, _value);
                    return;
                }

                MaxRetriesExceeded?.BeginRaiseEvent(this, exceptions);
            }
            finally
            {
                _updateIsInProgress.Value = false;
            }
        }
    }
}