using System;
using System.Threading;

namespace Abc.Zebus.MessageDsl.Support
{
    internal class Disposable : IDisposable
    {
        private Action? _onDispose;

        private Disposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public static IDisposable Create(Action onDispose) => new Disposable(onDispose);
        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}
