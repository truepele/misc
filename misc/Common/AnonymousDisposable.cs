using System;

namespace truepele.Common
{
    public class AnonymousDisposable : IDisposable
    {
        private readonly Action _onDispose;

        public AnonymousDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }
}