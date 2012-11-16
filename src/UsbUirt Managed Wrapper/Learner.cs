using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UsbUirt.Enums;
using UsbUirt.EventArgs;
using UsbUirt.State;

namespace UsbUirt
{
    public class Learner : DriverUserBase
    {
        private CodeFormat _defaultLearnCodeFormat;
        private LearnCodeModifier _defaultLearnCodeModifier;
        private readonly ConcurrentDictionary<object, LearnState> _learnStates = new ConcurrentDictionary<object, LearnState>();

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

        public Learner(CodeFormat defaultCodeFormat = CodeFormat.Pronto, 
                       LearnCodeModifier defaultLearnCodeModifier = LearnCodeModifier.Default)
        {
            _defaultLearnCodeFormat = defaultCodeFormat;
            _defaultLearnCodeModifier = defaultLearnCodeModifier;
        }

        public Learner(Driver driver,
                       CodeFormat defaultCodeFormat = CodeFormat.Pronto,
                       LearnCodeModifier defaultLearnCodeModifier = LearnCodeModifier.Default)
            : base(driver)
        {
            _defaultLearnCodeFormat = defaultCodeFormat;
            _defaultLearnCodeModifier = defaultLearnCodeModifier;
        }

        public event EventHandler<LearnCompletedEventArgs> LearnCompleted;

        public event EventHandler<LearningEventArgs> Learning;
        
        /// <summary>
        /// Delegate used as a callback during learning in order to update display the progress
        /// </summary>
        private delegate void LearnCallback(
            uint progress,
            uint sigQuality,
            uint carrierFreq,
            IntPtr userData);
        
        /// <summary>
        /// Learns an IR code synchronously.
        /// </summary>
        /// <param name="codeFormat">The format of the IR code to use in learning.</param>
        /// <param name="learnCodeFormat">The modifier used for the code format.</param>
        /// <param name="forcedFrequency">The frequency to use in learning.</param>
        /// <param name="timeout">The timeout after which to abort learning if it has not completed.</param>
        /// <returns>The IR code that was learned, or null if learning failed.</returns>
        public string Learn(
            CodeFormat? codeFormat = null,
            LearnCodeModifier? learnCodeFormat = null,
            uint? forcedFrequency = null,
            TimeSpan? timeout = null)
        {
            CheckDisposed();
            timeout = timeout ?? TimeSpan.Zero;
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("timeout", "timeout cannot be negative");
            }

            var learnTask = LearnAsync(codeFormat, learnCodeFormat, forcedFrequency);
            if (TimeSpan.Zero == timeout)
            {
                return learnTask.Result;
            }
            if (learnTask.Wait(timeout.Value))
            {
                if (learnTask.Exception != null)
                {
                    throw learnTask.Exception;
                }
                if (learnTask.IsCanceled)
                {
                    return null;
                }
                return learnTask.Result;
            }
            LearnAsyncCancel();
            return null;
        }
        
        /// <summary>
        /// Learns an IR code asynchronously.
        /// </summary>
        /// <param name="codeFormat">The format of the IR code to use in learning.</param>
        /// <param name="learnCodeModifier">The modifier used for the code format.</param>
        /// <param name="forcedFrequency">The frequency to use in learning.</param>
        /// <param name="userState">An optional user state object that will be passed to the 
        /// Learning and LearnCompleted events and which can be used when calling LearnAsyncCancel().</param>
        public Task<string> LearnAsync(
            CodeFormat? codeFormat = null,
            LearnCodeModifier? learnCodeModifier = null,
            uint? forcedFrequency = null,
            object userState = null)
        {
            CheckDisposed();
            if (learnCodeModifier == LearnCodeModifier.ForceFrequency)
            {
                if (forcedFrequency == 0)
                {
                    throw new ArgumentException("forcedFrequency must be specified when using LearnCodeModifier.ForceFrequency",
                        "forcedFrequency");
                }
            }
            else
            {
                if (forcedFrequency != null && forcedFrequency != 0)
                {
                    throw new ArgumentException("forcedFrequency can only be specified when using LearnCodeModifier.ForceFrequency",
                        "forcedFrequency");
                }
            }

            object learnStatesKey = userState ?? this;
            var cancellationSource = new CancellationTokenSource();
            var learnState = new LearnState(
                    codeFormat ?? _defaultLearnCodeFormat, 
                    learnCodeModifier ?? _defaultLearnCodeModifier, 
                    cancellationSource, 
                    forcedFrequency ?? 0, 
                    userState);
            _learnStates[learnStatesKey] = learnState;

            var learnTask = Task<string>.Factory.StartNew(LearnInternal, learnState, cancellationSource.Token);
            learnTask.ContinueWith(t => 
            {
                try
                {
                    var temp = LearnCompleted;
                    if (temp != null)
                    {
                        temp(this,
                            new LearnCompletedEventArgs(t.Exception,
                                learnState.WasAborted,
                                t.Result,
                                learnState.UserState));
                    }
                }
                finally
                {
                    learnState.Dispose();
                    learnStatesKey = null == learnState.UserState ? this : t.AsyncState;
                    _learnStates[learnStatesKey] = null;
                }
            });

            return learnTask;
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
            object learnStatesKey = userState ?? this;
            var learnState = _learnStates[learnStatesKey];
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
        //TODO: Hate the loss of type safety here
        private string LearnInternal(object state)
        {
            var learnState = (LearnState)state;
            var outCode = new StringBuilder(4096);
            var userDataHandle = new GCHandle();
            IntPtr userDataPtr = IntPtr.Zero;

            userDataHandle = GCHandle.Alloc(learnState);
            userDataPtr = (IntPtr)userDataHandle;

            if (!UUIRTLearnIR(
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
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            if (learnState.WasAborted)
            {
                learnState.CancelationToken.Cancel();
            }

            if (userDataHandle.IsAllocated)
            {
                userDataHandle.Free();
            }
            return outCode.ToString();
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
            var temp = Learning;
            if (null != temp)
            {
                temp(this, new LearningEventArgs(progress, sigQuality, carrierFreq, state));
            }
        }
    }
}