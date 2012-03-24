using System;
using System.Runtime.InteropServices;

namespace UsbUirt
{
    /// <summary>
    /// Creates an instance of the UUIRT driver.
    /// </summary>
    public class Driver : IDisposable
    {
        private const Int32 INVALID_HANDLE_VALUE = -1;
        private bool _disposed;

        /// <summary>
        /// Creates an instance of the UUIRT driver for use.
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public Driver()
        {
            Handle = UUIRTOpen();
            if (Handle.ToInt32() == INVALID_HANDLE_VALUE)
            {
                switch (Marshal.GetLastWin32Error())
                {
                    case (int)UuirtDriverError.NoDll:
                        throw new ApplicationException(
                            "Unable to find USB-UIRT Driver. Please make sure driver is Installed");

                    case (int)UuirtDriverError.NoDeviceFound:
                        throw new ApplicationException(
                            "Unable to connect to USB-UIRT device! Please ensure device is connected to the computer");

                    case (int)UuirtDriverError.NoResponse:
                        throw new ApplicationException(
                            "Unable to communicate with USB-UIRT device! Please check connections and try again.  If you still have problems, try unplugging and reconnecting your USB-UIRT.  If problem persists, contact Technical Support");

                    case (int)UuirtDriverError.WrongVersion:
                        throw new ApplicationException(
                            "Your USB-UIRT's firmware is not compatible with this API DLL. Please verify you are running the latest API DLL and that you're using the latest version of USB-UIRT firmware!  If problem persists, contact Technical Support");
                }

                throw new ApplicationException("Unable to initialize USB-UIRT (unknown error)");
            }
        }

        private Driver(IntPtr handle)
        {
            Handle = handle;
        }

        public static DriverVersion GetVersion(Driver driver)
        {
            uint driverVersion;
            var uuInfo = new UUINFO();
            try
            {
                if (!UUIRTGetDrvInfo(out driverVersion))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                if (!UUIRTGetUUIRTInfo(driver.Handle, ref uuInfo))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to read UsbUirt driver version", ex);
            }

            return new DriverVersion(
                driverVersion,
                new DateTime(2000 + uuInfo.fwDateYear, uuInfo.fwDateMonth, uuInfo.fwDateDay),
               uuInfo.fwVersion,
               uuInfo.protVersion);
        }

        public static DriverVersion GetVersion()
        {
            var handle = UUIRTOpen();
            DriverVersion version = GetVersion(new Driver(handle));
            UUIRTClose(handle);
            return version;
        }

        private enum UuirtDriverError
        {
            /// <summary>
            /// Unable to connect to USB-UIRT device
            /// </summary>
            NoDeviceFound = 0x20000001,

            /// <summary>
            /// Unable to communicate with USB-UIRT device
            /// </summary>
            NoResponse = 0x20000002,

            /// <summary>
            /// Unable to find USB-UIRT Driver
            /// </summary>
            NoDll = 0x20000003,

            /// <summary>
            /// USB-UIRT's firmware is not compatible with this API DLL
            /// </summary>
            WrongVersion = 0x20000004
        }

        /// <summary>
        /// The handle to the UUIRT driver.
        /// </summary>
        public IntPtr Handle { get; private set; }

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
                if (IntPtr.Zero != Handle)
                {
                    UUIRTClose(Handle);
                    Handle = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Terminates communication with the USB-UIRT. Should be called prior to terminating 
        /// host program.
        /// </summary>
        /// <param name="hDrvHandle">Handle to to USB-UIRT returned by UUIRTOpen</param>
        /// <returns></returns>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern bool UUIRTClose(IntPtr hDrvHandle);

        /// <summary>
        /// Opens communication with the USB-UIRT.  
        /// A call to UUIRTOpen should occur prior to any other driver function calls (with 
        /// the exception of UUIRTGetDrvInfo below).
        /// </summary>
        /// <returns>On success, a handle to be used in subsequent calls to USB-UIRT
        /// functions. On failure, INVALID_HANDLE_VALUE.</returns>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern IntPtr UUIRTOpen();

        /// <summary>
        /// Retrieves information about the UUIRT hardware.
        /// </summary>
        /// <param name="hDrvHandle">Handle to to USB-UIRT returned by UUIRTOpen</param>
        /// <param name="uuInfo">UUINFO structure that will be filled in upon success</param>
        /// <returns>TRUE on success</returns>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern bool UUIRTGetUUIRTInfo(IntPtr hDrvHandle, ref UUINFO uuInfo);

        /// <summary>
        /// Retrieves information about the driver (not the hardware itself). This is 
        /// intended to allow version control on the .DLL driver and accomodate future 
        /// changes and enhancements to the API. 
        /// </summary>
        /// <remarks>This call may be called prior to a call to UUIRTOpen.</remarks>
        /// <param name="drvVersion"></param>
        /// <returns>TRUE on success</returns>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern bool UUIRTGetDrvInfo(out uint drvVersion);

        /// <summary>
        /// Reperesents information about the UUIRT hardware.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct UUINFO
        {
            /// <summary>
            /// Version of firmware residing on the USB-UIRT.
            /// </summary>
            internal readonly uint fwVersion;

            /// <summary>
            /// Protocol version supported by the USB-UIRT firmware.
            /// </summary>
            internal readonly uint protVersion;

            /// <summary>
            /// Firmware revision day
            /// </summary>
            internal readonly byte fwDateDay;

            /// <summary>
            /// Firmware revision month
            /// </summary>
            internal readonly byte fwDateMonth;

            /// <summary>
            /// Firmware revision year
            /// </summary>
            internal readonly byte fwDateYear;
        }
    }
}