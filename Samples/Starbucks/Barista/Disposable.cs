using System;

namespace Barista
{
    public abstract class Disposable : IDisposable
    {
        protected Boolean IsDisposed { get; private set; }

        private void Dispose(Boolean disposing)
        {
            // The object has already been disposed
            if(IsDisposed)
            {
                return;
            }

            if(disposing)
            {
                // Dispose managed resources
                DisposeManagedResources();
            }

            // Dispose unmanaged resources
            DisposeUnmanagedResources();

            IsDisposed = true;
        }

        ~Disposable()
        {
            Dispose(false);
        }

        protected abstract void DisposeManagedResources();

        protected virtual void DisposeUnmanagedResources()
        { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
