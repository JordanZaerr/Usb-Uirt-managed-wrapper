using System;
using System.Runtime.InteropServices;
using System.Threading;
using UsbUirt.Enums;
using UsbUirt.EventArgs;
using UsbUirt.State;

namespace UsbUirt
{
    public class Transmitter : DriverUserBase
    {
        private int _defaultRepeatCount = 1;
        private TimeSpan _defaultInactivityWaitTime = TimeSpan.FromMilliseconds(50);
        private CodeFormat _defaultTransmitCodeFormat = CodeFormat.Pronto;

        public Transmitter()
        {
        }

        public Transmitter(Driver driver) : base(driver)
        {
        }

        public TimeSpan DefaultInactivityWaitTime 
        {
            get { return _defaultInactivityWaitTime; }
            set { _defaultInactivityWaitTime = value; }
        }

        public CodeFormat DefaultTransmitCodeFormat 
        {
            get { return _defaultTransmitCodeFormat; }
            set { _defaultTransmitCodeFormat = value; }
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
        /// Transmits an IR code synchronously using the default code format.
        /// </summary>
        /// <param name="irCode">The IR code to transmit.</param>
        public void Transmit(string irCode)
        {
            CheckDisposed();
            Transmit(irCode, DefaultTransmitCodeFormat, _defaultRepeatCount, _defaultInactivityWaitTime);
        }

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
        public void Transmit(
            string irCode,
            CodeFormat codeFormat,
            int repeatCount,
            TimeSpan inactivityWaitTime)
        {
            CheckDisposed();
            if (null == irCode)
            {
                throw new ArgumentNullException("irCode", "irCode cannot be null");
            }

            if (0 == irCode.Length)
            {
                throw new ArgumentException("irCode", "irCode cannot be empty");
            }

            if (repeatCount < 0)
            {
                throw new ArgumentOutOfRangeException("repeatCount", "repeatCount cannot be negative");
            }

            if (inactivityWaitTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("inactivityWaitTime",
                    "inactivityWaitTime cannot be less than TimeSpan.Zero");
            }

            using (var evt = new ManualResetEvent(false))
            {
                TransmitIr(irCode,
                    codeFormat,
                    repeatCount,
                    Convert.ToInt32(inactivityWaitTime.TotalMilliseconds),
                    evt);
                evt.WaitOne();
            }
        }

        /// <summary>
        /// Transmits an IR code asynchronously using the default code format.
        /// </summary>
        /// <param name="irCode">The IR code to transmit.</param>
        public void TransmitAsync(string irCode)
        {
            CheckDisposed();
            TransmitAsync(irCode,
                DefaultTransmitCodeFormat,
                _defaultRepeatCount,
                _defaultInactivityWaitTime,
                null);
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
        public void TransmitAsync(
            string irCode,
            CodeFormat codeFormat,
            int repeatCount,
            TimeSpan inactivityWaitTime)
        {
            CheckDisposed();
            TransmitAsync(irCode, codeFormat, repeatCount, inactivityWaitTime, null);
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
        /// <param name="userState">An optional user state object that will be passed to the 
        /// TransmitCompleted event.</param>
        public void TransmitAsync(
            string irCode,
            CodeFormat codeFormat,
            int repeatCount,
            TimeSpan inactivityWaitTime,
            object userState)
        {
            CheckDisposed();
            if (null == irCode)
            {
                throw new ArgumentNullException("irCode", "irCode cannot be null");
            }

            if (0 == irCode.Length)
            {
                throw new ArgumentException("irCode", "irCode cannot be empty");
            }

            if (repeatCount < 0)
            {
                throw new ArgumentOutOfRangeException("repeatCount", "repeatCount cannot be negative");
            }

            if (inactivityWaitTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("inactivityWaitTime",
                    "inactivityWaitTime cannot be less than TimeSpan.Zero");
            }

            var transmitState = new TransmitState(irCode,
                codeFormat,
                repeatCount,
                Convert.ToInt32(inactivityWaitTime.TotalMilliseconds),
                userState);
            if (false == ThreadPool.QueueUserWorkItem(DoTransmit, transmitState))
            {
                throw new ApplicationException("Unable to QueueUserWorkItem");
            }
        }

        private void DoTransmit(object state)
        {
            var transmitState = state as TransmitState;
            try
            {
                Exception error = null;
                try
                {
                    TransmitIr(
                        transmitState.IRCode,
                        transmitState.CodeFormat,
                        transmitState.RepeatCount,
                        transmitState.InactivityWaitTime,
                        transmitState.WaitEvent);
                    transmitState.WaitEvent.WaitOne();
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                var temp = TransmitCompleted;
                if (null != temp)
                {
                    temp(this, new TransmitCompletedEventArgs(error, transmitState.UserState));
                }
            }
            finally
            {
                transmitState.Dispose();
            }
        }

        private void TransmitIr(
            string irCode,
            CodeFormat codeFormat,
            int repeatCount,
            int inactivityWaitTime,
            ManualResetEvent evt)
        {
            if (false == UUIRTTransmitIR(
                DriverHandle,
                irCode,
                (int)codeFormat,
                repeatCount,
                inactivityWaitTime,
                null == evt ? IntPtr.Zero : evt.Handle,
                IntPtr.Zero,
                IntPtr.Zero))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
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