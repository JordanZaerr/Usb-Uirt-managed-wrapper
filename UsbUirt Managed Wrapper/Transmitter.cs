using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UsbUirt.Enums;
using UsbUirt.EventArgs;
namespace UsbUirt
{
    public class Transmitter : DriverUserBase
    {
        private int _defaultRepeatCount;
        private int _defaultInactivityWaitTime;
        private CodeFormat _defaultCodeFormat;
        private Emitter _defaultEmitter;

        /// <summary>
        /// Creates an instance of the Transmitter class that can be used to transmit IR codes.
        /// </summary>
        /// <param name="defaultEmitter">The emitter to transmit the IR code with</param>
        /// <param name="defaultCodeFormat">The format of the IR code.</param>
        /// <param name="defaultRepeatCount">Indicates how many iterations of the code should be 
        /// sent (in the case of a 2-piece code, the first stream is sent once followed 
        /// by the second stream sent repeatCount times).</param>
        /// <param name="defaultInactivityWaitTime">Time in milliseconds since the last received 
        /// IR activity to wait before sending an IR code. Normally, pass 0 for this parameter.</param>
        /// <remarks>This class should be disposed if using this constructor.</remarks>
        public Transmitter(Emitter defaultEmitter = Emitter.All, 
                           CodeFormat defaultCodeFormat = CodeFormat.Pronto,
                           int defaultRepeatCount = 1,
                           int defaultInactivityWaitTime = 0)
        {
            _defaultEmitter = defaultEmitter;
            _defaultCodeFormat = defaultCodeFormat;
            _defaultRepeatCount = defaultRepeatCount;
            _defaultInactivityWaitTime = defaultInactivityWaitTime;
        }

        /// <summary>
        /// Creates an instance of the Transmitter class that can be used to transmit IR codes.
        /// </summary>
        /// <param name="driver">An instance of a driver that can be shared among components.</param>
        /// <param name="defaultEmitter">The emitter to transmit the IR code with</param>
        /// <param name="defaultCodeFormat">The format of the IR code.</param>
        /// <param name="defaultRepeatCount">Indicates how many iterations of the code should be 
        /// sent (in the case of a 2-piece code, the first stream is sent once followed 
        /// by the second stream sent repeatCount times).</param>
        /// <param name="defaultInactivityWaitTime">Time in milliseconds since the last received 
        /// IR activity to wait before sending an IR code. Normally, pass 0 for this parameter.</param>
        public Transmitter(Driver driver, 
                           Emitter defaultEmitter = Emitter.All,
                           CodeFormat defaultCodeFormat = CodeFormat.Pronto,
                           int defaultRepeatCount = 1,
                           int defaultInactivityWaitTime = 0)
            : base(driver)
        {
            _defaultEmitter = defaultEmitter;
            _defaultCodeFormat = defaultCodeFormat;
            _defaultRepeatCount = defaultRepeatCount;
            _defaultInactivityWaitTime = defaultInactivityWaitTime;
        }

        public Emitter DefaultEmitter
        {
            get { return _defaultEmitter; }
            set { _defaultEmitter = value; }
        }

        public int DefaultInactivityWaitTime 
        {
            get { return _defaultInactivityWaitTime; }
            set { _defaultInactivityWaitTime = value; }
        }

        public CodeFormat DefaultCodeFormat 
        {
            get { return _defaultCodeFormat; }
            set { _defaultCodeFormat = value; }
        }

        public int DefaultRepeatCount 
        {
            get { return _defaultRepeatCount; }
            set { _defaultRepeatCount = value; }
        }

        public bool BlinkOnTransmit
        {
            get
            {
                return GeneralSettings.GetBlinkOnTransmit(DriverHandle);
            }
            set
            {
                GeneralSettings.SetBlinkOnTransmit(DriverHandle, value);
            }
        }

        /// <summary>
        /// Raised when transmission, begun via TransmitAsync(), has completed.
        /// </summary>
        public event EventHandler<TransmitCompletedEventArgs> TransmitCompleted;

        /// <summary>
        /// Transmits an IR code synchronously.
        /// </summary>
        /// <param name="irCode">The IR code to transmit.</param>
        /// <param name="codeFormat">The format of the IR code.</param>
        /// <param name="repeatCount">Indicates how many iterations of the code should be 
        /// sent (in the case of a 2-piece code, the first stream is sent once followed 
        /// by the second stream sent repeatCount times).</param>
        /// <param name="inactivityWaitTime">Time in milliseconds since the last received 
        /// IR activity to wait before sending an IR code. Normally, pass 0 for this parameter.</param>
        /// <param name="emitter">The emitter to transmit the IR code with</param>
        public void Transmit(
            string irCode,
            Emitter? emitter = null,
            CodeFormat? codeFormat = null,
            int? repeatCount = null,
            int? inactivityWaitTime = null)
        {
            CheckDisposed();
            var task = Task.Factory.StartNew(() => TransmitInternal(irCode,
                    codeFormat,
                    repeatCount,
                    inactivityWaitTime,
                    emitter));
            try
            {
                task.Wait();
            }
            catch (Exception ex)
            {
                throw ex.GetBaseException();
            }
        }

        /// <summary>
        /// Transmits an IR code asynchronously.
        /// </summary>
        /// <param name="irCode">The IR code to transmit.</param>
        /// <param name="codeFormat">The format of the IR code.</param>
        /// <param name="repeatCount">Indicates how many iterations of the code should be 
        /// sent (in the case of a 2-piece code, the first stream is sent once followed 
        /// by the second stream sent repeatCount times).</param>
        /// <param name="inactivityWaitTime">Time in milliseconds since the last received 
        /// IR activity to wait before sending an IR code. Normally, pass 0 for this parameter.</param>
        /// <param name="emitter">The emitter to transmit the IR code with</param>
        /// <param name="userState">An optional user state object that will be passed to the 
        /// TransmitCompleted event.</param>
        public void TransmitAsync(
            string irCode,
            object userState = null,
            Emitter? emitter = null,
            CodeFormat? codeFormat = null,
            int? repeatCount = null,
            int? inactivityWaitTime = null)
        {
            CheckDisposed();
            Task.Factory
                .StartNew(() => TransmitInternal(irCode,
                    codeFormat,
                    repeatCount,
                   inactivityWaitTime,
                    emitter))
                .ContinueWith(t =>
                {
                    var temp = TransmitCompleted;
                    if (null != temp)
                    {
                        temp(this, new TransmitCompletedEventArgs(t.Exception, userState));
                    }
                });
        }

        [SecuritySafeCritical]
        private void TransmitInternal(
            string irCode,
            CodeFormat? codeFormat,
            int? repeatCount,
            int? inactivityWaitTime,
            Emitter? emitter)
        {
            codeFormat = codeFormat ?? _defaultCodeFormat;
            repeatCount = repeatCount ?? _defaultRepeatCount;
            inactivityWaitTime = inactivityWaitTime ?? _defaultInactivityWaitTime;
            emitter = emitter ?? _defaultEmitter;

            if (irCode == null)
            {
                throw new ArgumentNullException("irCode", "irCode cannot be null");
            }

            if (irCode.Length == 0)
            {
                throw new ArgumentException("irCode", "irCode cannot be empty");
            }

            if (repeatCount < 0)
            {
                throw new ArgumentOutOfRangeException("repeatCount", "repeatCount cannot be negative");
            }

            if (inactivityWaitTime < 0)
            {
                throw new ArgumentOutOfRangeException("inactivityWaitTime",
                    "inactivityWaitTime cannot be less than TimeSpan.Zero");
            }

            using (var evt = new ManualResetEvent(false))
            {
                if (false == UUIRTTransmitIR(
                    DriverHandle,
                    emitter.Value.GetZoneForEmitter() + irCode,
                    (int)codeFormat.Value,
                    repeatCount.Value,
                    inactivityWaitTime.Value,
                    evt.Handle,
                    IntPtr.Zero,
                    IntPtr.Zero))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                evt.WaitOne();
            }
        }

        /// <summary>
        /// Transmits an IR code via the USB-UIRT hardware.
        /// </summary>
        /// <param name="hDrvHandle">Handle to to USB-UIRT returned by UUIRTOpen</param>
        /// <param name="irCode">null-terminated string</param>
        /// <param name="codeFormat">format specifier which identifies the format of the IRCode
        /// code. Currently, supported formats are Compressed_UIRT (STRUCT), RAW, and 
        /// Pronto-RAW</param>
        /// <param name="repeatCount">indicates how many iterations of the code should be 
        /// sent (in the case of a 2-piece code, the first stream is sent once followed 
        /// by the second stream sent repeatCount times)</param>
        /// <param name="inactivityWaitTime">time 
        /// in milliseconds since the last received IR activity to wait before sending an 
        /// IR code -- normally pass 0 for this parameter</param>
        /// <param name="hEvent">optional event handle which is obtained by a call to 
        /// CreateEvent. If hEvent is NULL, the call to UUIRTTransmitIR will block and not 
        /// return until the IR code has been fully transmitted to the air. If hEvent 
        /// is not NULL, it must be a valid Windows event hande. In this case, 
        /// UUIRTTransmitIR will return immediately and when the IR stream has 
        /// completed transmission this event will be signalled by the driver</param>
        /// <param name="reserved0">reserved for future expansion; should be NULL</param>
        /// <param name="reserved1">reserved for future expansion; should be NULL</param>
        /// <returns>TRUE on success</returns>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern bool UUIRTTransmitIR(
            IntPtr hDrvHandle,
            string irCode,
            int codeFormat,
            int repeatCount,
            int inactivityWaitTime,
            IntPtr hEvent,
            IntPtr reserved0,
            IntPtr reserved1);
    }
}