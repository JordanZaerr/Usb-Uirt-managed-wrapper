using System;
using System.Threading;
using UsbUirt.EventArgs;

namespace UsbUirt.State
{
    /// <summary>
    /// Summary description for SyncLearnResults.
    /// </summary>
    internal class SyncLearnResults : IDisposable
    {
        private bool _disposed;
        private LearnCompletedEventArgs _learnedEventArgs;
        private ManualResetEvent _manualResetEvent;

        internal SyncLearnResults()
        {
            _manualResetEvent = new ManualResetEvent(false);
        }

        internal LearnCompletedEventArgs LearnCompletedEventArgs
        {
            get { return _learnedEventArgs; }
            set { _learnedEventArgs = value; }
        }

        internal ManualResetEvent WaitEvent
        {
            get { return _manualResetEvent; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // Dispose any managed resources.
                }

                if (null != _manualResetEvent)
                {
                    _manualResetEvent.Close();
                    _manualResetEvent = null;
                }
            }
            _disposed = true;
        }

        #endregion
    }
}