using System;

namespace UsbUirt
{
    public abstract class DisposableObject : IDisposable
    {
        private bool _disposed;

        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("The object has already been disposed");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _disposed = true;
                InternalDispose();
            }
        }

        protected abstract void InternalDispose();
    }
}