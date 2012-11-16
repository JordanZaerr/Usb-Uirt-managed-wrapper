using System;

namespace UsbUirt
{
    public abstract class DriverUserBase : IDisposable
    {
        private readonly Driver _driver;
        private readonly bool _ownDriver;
        protected IntPtr DriverHandle 
        {
            get { return _driver.Handle; }
        }

        protected DriverUserBase()
        {
            _driver = new Driver();
            _ownDriver = true;
        }

        protected DriverUserBase(Driver driver)
        {
            _driver = driver;
        }

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
            if (isDisposing && !_disposed)
            {
                _disposed = true;
                InternalDispose();
                if (_ownDriver)
                {
                    _driver.Dispose();
                }
            }
        }

        protected virtual void InternalDispose()
        {
        }
    }
}