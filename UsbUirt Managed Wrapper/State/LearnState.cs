using System;
using System.Runtime.InteropServices;
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
        private readonly LearnCodeModifier _learnCodeFormat;
        private readonly object _userState;
        private IntPtr _abort;
        private bool _disposed;

        internal LearnState(
            CodeFormat codeFormat,
            LearnCodeModifier learnCodeFormat,
            uint forcedFrequency,
            object userState)
        {
            _codeFormat = codeFormat;
            _learnCodeFormat = learnCodeFormat;
            _forcedFrequency = forcedFrequency;
            _userState = userState;
            _abort = Marshal.AllocHGlobal(Marshal.SizeOf(typeof (Int32)));
            Marshal.WriteInt32(_abort, 0);
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
            get { return _learnCodeFormat; }
        }

        internal object UserState
        {
            get { return _userState; }
        }

        internal bool WasAborted
        {
            get { return Marshal.ReadInt32(_abort) != 0; }
        }

        internal void Abort()
        {
            Marshal.WriteInt32(_abort, 1);
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