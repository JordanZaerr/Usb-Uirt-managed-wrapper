using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using UsbUirt.Enums;

namespace UsbUirt.State
{
    /// <summary>
    /// Summary description for LearnState.
    /// </summary>
    internal class LearnState : IDisposable
    {
        private readonly CodeFormat _codeFormat;
        private readonly uint _forcedFrequency;
        private readonly LearnCodeModifier _learnCodeModifier;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly object _userState;
        private IntPtr _abort;
        private bool _disposed;

        [SecuritySafeCritical]
        internal LearnState(
            CodeFormat codeFormat,
            LearnCodeModifier learnCodeModifier,
            CancellationTokenSource cancellationSource,
            uint forcedFrequency,
            object userState)
        {
            _codeFormat = codeFormat;
            _learnCodeModifier = learnCodeModifier;
            _cancellationSource = cancellationSource;
            _forcedFrequency = forcedFrequency;
            _userState = userState;
            _abort = Marshal.AllocHGlobal(Marshal.SizeOf(typeof (Int32)));
            Marshal.WriteInt32(_abort, 0);
        }

        internal CancellationTokenSource CancelationToken 
        {
            get { return _cancellationSource; }
        }

        internal IntPtr AbortFlag
        {
            get { return _abort; }
        }

        internal CodeFormat CodeFormat
        {
            get { return _codeFormat; }
        }

        internal uint ForcedFrequency
        {
            get { return _forcedFrequency; }
        }

        internal LearnCodeModifier LearnCodeModifier
        {
            get { return _learnCodeModifier; }
        }

        internal object UserState
        {
            get { return _userState; }
        }

        internal bool WasAborted
        {
            get { return GetInt() != 0; }
        }

        [SecuritySafeCritical]
        public int GetInt()
        {
            return Marshal.ReadInt32(_abort);
        }

        [SecuritySafeCritical]
        public void SetInt()
        {
            Marshal.WriteInt32(_abort, 1);
        }

        internal void Abort()
        {
            SetInt();
            _cancellationSource.Cancel();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SecuritySafeCritical]
        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // Dispose any managed resources.
                }

                if (IntPtr.Zero != _abort)
                {
                    Marshal.FreeHGlobal(_abort);
                    _abort = IntPtr.Zero;
                }
            }
            _disposed = true;
        }

        #endregion
    }
}