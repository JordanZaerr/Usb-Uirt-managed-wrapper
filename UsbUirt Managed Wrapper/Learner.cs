using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using UsbUirt.Enums;
using UsbUirt.EventArgs;
using UsbUirt.State;

namespace UsbUirt
{
    public class Learner : DriverUserBase
    {
        private CodeFormat _defaultLearnCodeFormat = CodeFormat.Pronto;
        private LearnCodeModifier _defaultLearnCodeModifier = LearnCodeModifier.Default;
        private readonly Hashtable _learnStates = new Hashtable();

        public CodeFormat DefaultCodeFormat
        {
            get { return _defaultLearnCodeFormat; }
            set { _defaultLearnCodeFormat = value; }
        }

        public bool BlinkOnReceive 
        {
            get 
            {
                return GeneralSettings.GetBlinkOnReceive(DriverHandle);
            }
            set 
            {
                GeneralSettings.SetBlinkOnReceiver(DriverHandle, value);
            }
        }

        public LearnCodeModifier DefaultLearnCodeModifier 
        {
            get { return _defaultLearnCodeModifier; }
            set { _defaultLearnCodeModifier = value; }
        }

        public Learner()
        {
        }

        public Learner(Driver driver) : base(driver)
        {
        }


        /// <summary>
        /// The delegate used for the LarnCompleted event.
        /// </summary>
        public delegate void LearnCompletedEventHandler(object sender, LearnCompletedEventArgs e);

        /// <summary>
        /// The delegate used for the Learning event.
        /// </summary>
        public delegate void LearningEventHandler(object sender, LearningEventArgs e);

        /// <summary>
        /// Delegate used as a callback during learning in order to update display the progress
        /// </summary>
        private delegate void LearnCallback(
            uint progress,
            uint sigQuality,
            uint carrierFreq,
            IntPtr userData);

        /// <summary>
        /// Raised when learning, begun via LearnAsync(), has completed.
        /// </summary>
        public event LearnCompletedEventHandler LearnCompleted;

        /// <summary>
        /// Raised periodically during learning, to provided feedback on progress.
        /// </summary>
        public event LearningEventHandler Learning;

        /// <summary>
        /// Learns an IR code synchronously using the default code format.
        /// </summary>
        /// <returns>The IR code that was learned, or null if learning failed.</returns>
        public string Learn()
        {
            CheckDisposed();
            return Learn(_defaultLearnCodeFormat, _defaultLearnCodeModifier, TimeSpan.Zero);
        }

        /// <summary>
        /// Learns an IR code synchronously.
        /// </summary>
        /// <param name="codeFormat">The format of the IR code to use in learning.</param>
        /// <returns>The IR code that was learned, or null if learning failed.</returns>
        public string Learn(CodeFormat codeFormat)
        {
            CheckDisposed();
            return Learn(codeFormat, _defaultLearnCodeModifier, TimeSpan.Zero);
        }

        /// <summary>
        /// Learns an IR code synchronously using the default code format.
        /// </summary>
        /// <param name="timeout">The timeout after which to abort learning if it has not completed.</param>
        /// <returns>The IR code that was learned, or null if learning failed.</returns>
        public string Learn(TimeSpan timeout)
        {
            CheckDisposed();
            return Learn(_defaultLearnCodeFormat, _defaultLearnCodeModifier, timeout);
        }

        /// <summary>
        /// Learns an IR code synchronously.
        /// </summary>
        /// <param name="codeFormat">The format of the IR code to use in learning.</param>
        /// <param name="learnCodeFormat">The modifier used for the code format.</param>
        /// <param name="timeout">The timeout after which to abort learning if it has not completed.</param>
        /// <returns>The IR code that was learned, or null if learning failed.</returns>
        public string Learn(CodeFormat codeFormat, LearnCodeModifier learnCodeFormat, TimeSpan timeout)
        {
            CheckDisposed();
            return Learn(codeFormat, learnCodeFormat, 0, timeout);
        }

        /// <summary>
        /// Learns an IR code synchronously.
        /// </summary>
        /// <param name="codeFormat">The format of the IR code to use in learning.</param>
        /// <param name="learnCodeFormat">The modifier used for the code format.</param>
        /// <param name="forcedFrequency">The frequency to use in learning.</param>
        /// <param name="timeout">The timeout after which to abort learning if it has not completed.</param>
        /// <returns>The IR code that was learned, or null if learning failed.</returns>
        public string Learn(
            CodeFormat codeFormat,
            LearnCodeModifier learnCodeFormat,
            uint forcedFrequency,
            TimeSpan timeout)
        {
            CheckDisposed();
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("timeout", "timeout cannot be negative");
            }

            using (var results = new SyncLearnResults())
            {
                this.LearnCompleted += ManagedWrapper_LearnCompleted;

                try
                {
                    LearnAsync(codeFormat, learnCodeFormat, forcedFrequency, results);
                    if (TimeSpan.Zero == timeout)
                    {
                        results.WaitEvent.WaitOne();
                        return results.LearnCompletedEventArgs.Code;
                    }
                    else if (results.WaitEvent.WaitOne(timeout, false))
                    {
                        if (null != results.LearnCompletedEventArgs.Error)
                        {
                            throw results.LearnCompletedEventArgs.Error;
                        }
                        else if (results.LearnCompletedEventArgs.Cancelled)
                        {
                            return null;
                        }
                        return results.LearnCompletedEventArgs.Code;
                    }
                    else
                    {
                        LearnAsyncCancel(results);
                        return null;
                    }
                }
                finally
                {
                    this.LearnCompleted -= ManagedWrapper_LearnCompleted;
                }
            }
        }

        /// <summary>
        /// Learns an IR code asynchronously using the default code format.
        /// </summary>
        public void LearnAsync()
        {
            CheckDisposed();
            LearnAsync(_defaultLearnCodeFormat, _defaultLearnCodeModifier, null);
        }

        /// <summary>
        /// Learns an IR code asynchronously.
        /// </summary>
        /// <param name="codeFormat">The format of the IR code to use in learning.</param>
        public void LearnAsync(CodeFormat codeFormat)
        {
            CheckDisposed();
            LearnAsync(codeFormat, _defaultLearnCodeModifier, null);
        }

        /// <summary>
        /// Learns an IR code asynchronously.
        /// </summary>
        /// <param name="codeFormat">The format of the IR code to use in learning.</param>
        /// <param name="learnCodeFormat">The modifier used for the code format.</param>
        /// <param name="userState">An optional user state object that will be passed to the 
        /// Learning and LearnCompleted events and which can be used when calling LearnAsyncCancel().</param>
        public void LearnAsync(CodeFormat codeFormat, LearnCodeModifier learnCodeFormat, object userState)
        {
            CheckDisposed();
            LearnAsync(codeFormat, learnCodeFormat, 0, userState);
        }

        /// <summary>
        /// Learns an IR code asynchronously.
        /// </summary>
        /// <param name="codeFormat">The format of the IR code to use in learning.</param>
        /// <param name="learnCodeFormat">The modifier used for the code format.</param>
        /// <param name="forcedFrequency">The frequency to use in learning.</param>
        /// <param name="userState">An optional user state object that will be passed to the 
        /// Learning and LearnCompleted events and which can be used when calling LearnAsyncCancel().</param>
        public void LearnAsync(
            CodeFormat codeFormat,
            LearnCodeModifier learnCodeFormat,
            uint forcedFrequency,
            object userState)
        {
            CheckDisposed();
            if (LearnCodeModifier.ForceFrequency == learnCodeFormat)
            {
                if (0 == forcedFrequency)
                {
                    throw new ArgumentException("forcedFrequency",
                        "forcedFrequency must be specified when using LearnCodeModifier.ForceFrequency");
                }
            }
            else
            {
                if (0 != forcedFrequency)
                {
                    throw new ArgumentException("forcedFrequency",
                        "forcedFrequency can only be specified when using LearnCodeModifier.ForceFrequency");
                }
            }

            object learnStatesKey = null == userState ? this : userState;
            var learnState = new LearnState(codeFormat, learnCodeFormat, forcedFrequency, userState);
            _learnStates[learnStatesKey] = learnState;

            if (false == ThreadPool.QueueUserWorkItem(DoLearn, learnState))
            {
                throw new ApplicationException("Unable to QueueUserWorkItem");
            }
        }

        /// <summary>
        /// Cancels a LearnAsync() operation.
        /// </summary>
        public void LearnAsyncCancel()
        {
            CheckDisposed();
            LearnAsyncCancel(null);
        }

        /// <summary>
        /// Cancels a LearnAsync() operation that was passed the specified userState.
        /// </summary>
        /// <param name="userState">The optional userState object passed to LearnAsync().</param>
        public bool LearnAsyncCancel(object userState)
        {
            CheckDisposed();
            object learnStatesKey = null == userState ? this : userState;
            var learnState = _learnStates[learnStatesKey] as LearnState;
            if (null != learnState)
            {
                learnState.Abort();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Instructs the USB-UIRT and the API to learn an IR code.
        /// </summary>
        /// <param name="hDrvHandle">Handle to to USB-UIRT returned by UUIRTOpen</param>
        /// <param name="codeFormat">format specifier which identifies the format of the IRCode
        /// code to learn. Currently, supported formats are Compressed_UIRT (STRUCT), RAW, and 
        /// Pronto-RAW</param>
        /// <param name="IRCode">the learned IR code (upon return). It is the responsibility 
        /// of the caller to allocate space for this string; suggested string size is at 
        /// least 2048 bytes</param>
        /// <param name="progressProc">a caller-supplied callback function which will be called
        /// periodically during the learn process and may be used to update user dialogs, 
        /// etc. Information passed to the callback are learn progress %, signal quality, and 
        /// carrier frequency.</param>
        /// <param name="userData">will be passed by the USB-UIRT driver to any calls of 
        /// progressProc. </param>
        /// <param name="pAbort">pointer to a Boolean variable 
        /// which should be initialized to FALSE (0) prior to the call. Setting this variable 
        /// TRUE during the learn process will cause the UUIRTLearnIR process to abort and the 
        /// function to return. Since the UUIRTLearnIR function will block for the duration of 
        /// the learn process, one could set the *pAbort to TRUE either within the callback 
        /// function or from another thread</param>
        /// <param name="param1">currently used only when the codeFormat 
        /// includes the UUIRTDRV_IRFMT_LEARN_FORCEFREQ flag (not normally needed) -- in which 
        /// case param1 should indicate the forced carrier frequency</param>
        /// <param name="reserved0">reserved for future expansion; should be NULL</param>
        /// <param name="reserved1">reserved for future expansion; should be NULL</param>
        /// <returns>TRUE on success</returns>
        /// <remarks>The IR code learned  will be a complete IR stream suitable for subsequent 
        /// transmission via UUIRTTransmitIR. Consequently, the same formats supported by 
        /// Transmit are also available for learn. It is recommended to use either RAW or 
        /// Pronto-RAW codeFormat to offer the best compatibility; compressed-UIRT format 
        /// is often too limiting, although it does produce the smallest codes.</remarks>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern bool UUIRTLearnIR(
            IntPtr hDrvHandle,
            int codeFormat,
            StringBuilder IRCode,
            LearnCallback progressProc,
            IntPtr userData,
            IntPtr pAbort,
            uint param1,
            IntPtr reserved0,
            IntPtr reserved1);

        [SecuritySafeCritical]
        private void DoLearn(object state)
        {
            var learnState = state as LearnState;
            var outCode = new StringBuilder(4096);
            var userDataHandle = new GCHandle();
            IntPtr userDataPtr = IntPtr.Zero;

            try
            {
                userDataHandle = GCHandle.Alloc(learnState);
                userDataPtr = (IntPtr)userDataHandle;
                Exception error = null;

                if (false == UUIRTLearnIR(
                    DriverHandle,
                    (int)learnState.CodeFormat | (int)learnState.LearnCodeModifier,
                    outCode,
                    LearnCallbackProc,
                    userDataPtr,
                    learnState.AbortFlag,
                    learnState.ForcedFrequency,
                    IntPtr.Zero,
                    IntPtr.Zero))
                {
                    try
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                }

                if (userDataHandle.IsAllocated)
                {
                    userDataHandle.Free();
                }

                LearnCompletedEventHandler temp = LearnCompleted;
                if (null != temp)
                {
                    temp(this,
                        new LearnCompletedEventArgs(error,
                            learnState.WasAborted,
                            outCode.ToString(),
                            learnState.UserState));
                }
            }
            finally
            {
                learnState.Dispose();
                object learnStatesKey = null == learnState.UserState ? this : state;
                _learnStates[learnStatesKey] = null;
            }
        }

        [SecuritySafeCritical]
        private void LearnCallbackProc(
            uint progress,
            uint sigQuality,
            uint carrierFreq,
            IntPtr userState)
        {
            var userDataHandle = (GCHandle)userState;
            object state = userDataHandle.Target;
            LearningEventHandler temp = Learning;
            if (null != temp)
            {
                temp(this, new LearningEventArgs(progress, sigQuality, carrierFreq, state));
            }
        }

        private void ManagedWrapper_LearnCompleted(object sender, LearnCompletedEventArgs e)
        {
            var syncLearnResults = e.UserState as SyncLearnResults;
            if (null == syncLearnResults)
            {
                throw new ApplicationException("invalid userState received");
            }
            syncLearnResults.LearnCompletedEventArgs = e;
            syncLearnResults.WaitEvent.Set();
        }
    }
}