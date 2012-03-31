using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using UsbUirt.EventArgs;

namespace UsbUirt
{
    public class Receiver : DriverUserBase
    {
        private ReceiveCallback _receiveCallback;
        private ReceivedEventHandler _received;
        private readonly object _receivedSyncRoot = new object();

        public Receiver()
        {
            GenerateLegacyCodes = false;
        }

        public Receiver(Driver driver) : base(driver)
        {
            GenerateLegacyCodes = false;
        }

        public bool GenerateLegacyCodes 
        {
            get { return GeneralSettings.GetGenerateLegacyCodesOnReceive(DriverHandle); }
            set { GeneralSettings.SetGenerateLegacyCodesOnReceive(DriverHandle, value); }
        }

        /// <summary>
        /// The delegate used for the Received event.
        /// </summary>
        public delegate void ReceivedEventHandler(object sender, ReceivedEventArgs e);

        /// <summary>
        /// Raised when IR input is received.
        /// </summary>
        /// 
        public event ReceivedEventHandler Received
        {
            add
            {
                CheckDisposed();
                lock (_receivedSyncRoot)
                {
                    if (null == _received)
                    {
                        _receiveCallback = ReceiveCallbackProc;
                        SetReceiveCallback(_receiveCallback);
                    }
                    _received += value;
                }
            }
            remove
            {
                CheckDisposed();
                lock (_receivedSyncRoot)
                {
                    _received -= value;
                    if (_received == null)
                    {
                        ClearReceiveCallback();
                        GC.KeepAlive(_receiveCallback);
                    }
                }
            }
        }

        private void ReceiveCallbackProc(StringBuilder irEventString, IntPtr userState)
        {
            ReceivedEventHandler temp = _received;
            if (null != temp)
            {
                temp(this, new ReceivedEventArgs(irEventString.ToString()));
            }
        }

        [SecuritySafeCritical]
        private void ClearReceiveCallback()
        {
            if (false == UUIRTSetReceiveCallback(DriverHandle, null, IntPtr.Zero))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        [SecuritySafeCritical]
        private void SetReceiveCallback(ReceiveCallback cb)
        {
            if (false
                ==
                UUIRTSetReceiveCallback(DriverHandle,
                    cb,
                    IntPtr.Zero))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        /// <summary>
        /// Delegate used to receive callbacks when IR input is received
        /// </summary>
        private delegate void ReceiveCallback(
            StringBuilder IREventStr,
            IntPtr userData);

        /// <summary>
        /// Registers a receive callback function which the driver will call when an IR code 
        /// is received from the air.
        /// 
        /// typedef void (WINAPI *PUUCALLBACKPROC) (char *IREventStr, void *userData);
        /// When the USB-UIRT receives a code from the air, it will call the callback function
        /// with a null-terminated, twelve-character (like IRMAN) ir code in IREventStr. 
        /// </summary>
        /// <remarks>
        /// The types of codes which are passed to IREventStr are not the same as the type
        /// of codes passed back from a UUIRTLearnIR call (the codes from a UUIRTLearnIR 
        /// are much larger and contain all the necessary data to reproduce a code, 
        /// whereas the codes passed to IREventStr are simpler representations of IR codes 
        /// only long enough to be unique).
        /// </remarks>
        /// <param name="hDrvHandle">Handle to to USB-UIRT returned by UUIRTOpen</param>
        /// <param name="receiveProc">the address of a 
        /// PUUCALLBACKPROC function</param>
        /// <param name="userData">a general-purpose 
        /// 32-bit value supplied by the caller to UUIRTSetReceiveCallback. This parameter 
        /// is useful for carrying context information, etc. Will be passed to receiveProc.</param>
        /// <returns>TRUE on success</returns>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern bool UUIRTSetReceiveCallback(
            IntPtr hDrvHandle,
            ReceiveCallback receiveProc,
            IntPtr userData);
    }
}