using System;
using System.Runtime.InteropServices;

namespace UsbUirt
{
    internal static class GeneralSettings
    {

        public static bool GetBlinkOnReceive(IntPtr driverHandle)
        {
            return (GetConfig(driverHandle) & UUIRTConfigBits.BlinkOnReceive) != 0;
        }

        public static void SetBlinkOnReceiver(IntPtr driverHandle, bool value)
        {
            SetConfig(driverHandle, value,
                GetSetting(driverHandle, UUIRTConfigBits.BlinkOnTransmit),
                GetSetting(driverHandle, UUIRTConfigBits.GenerateLegacyCodesOnReceive));
        }

        public static bool GetBlinkOnTransmit(IntPtr driverHandle)
        {
            return (GetConfig(driverHandle) & UUIRTConfigBits.BlinkOnTransmit) != 0;
        }

        public static void SetBlinkOnTransmit(IntPtr driverHandle, bool value)
        {
            SetConfig(driverHandle, 
                GetSetting(driverHandle, UUIRTConfigBits.BlinkOnReceive),
                value,
                GetSetting(driverHandle, UUIRTConfigBits.GenerateLegacyCodesOnReceive));
        }

        public static bool GetGenerateLegacyCodesOnReceive(IntPtr driverHandle)
        {
            return (GetConfig(driverHandle) & UUIRTConfigBits.GenerateLegacyCodesOnReceive) != 0;
        }

        public static void SetGenerateLegacyCodesOnReceive(IntPtr driverHandle, bool value)
        {
            SetConfig(driverHandle, 
                GetSetting(driverHandle, UUIRTConfigBits.BlinkOnReceive),
                GetSetting(driverHandle, UUIRTConfigBits.BlinkOnTransmit),
                value);
        }

        private static bool GetSetting(IntPtr driverHandle, UUIRTConfigBits bit)
        {
            return (GetConfig(driverHandle) & bit) != 0;
        }

        public static bool BlinkOnTransmit { get; set; }

        public static bool GenerateLegacyCodesOnReceive { get; set; }

        private static UUIRTConfigBits GetConfig(IntPtr driverHandle)
        {
            uint uConfig;
            if (false == UUIRTGetUUIRTConfig(driverHandle, out uConfig))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return ((UUIRTConfigBits)uConfig);
        }

        private static void SetConfig(
            IntPtr driverHandle,
            bool blinkOnReceive,
            bool blinkOnTransmit,
            bool generateLegacyCodesOnReceive)
        {
            UUIRTConfigBits uConfig =
                (blinkOnReceive ? UUIRTConfigBits.BlinkOnReceive : 0) |
                (blinkOnTransmit ? UUIRTConfigBits.BlinkOnTransmit : 0) |
                (generateLegacyCodesOnReceive ? UUIRTConfigBits.GenerateLegacyCodesOnReceive : 0);

            if (false == UUIRTSetUUIRTConfig(driverHandle, (uint)uConfig))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        /// <summary>
        /// Flags used when getting or setting the USB-UIRT configuration
        /// </summary>
        [Flags]
        private enum UUIRTConfigBits : uint
        {
            /// <summary>
            /// Indicator LED on USB-UIRT blinks when remote signals are received
            /// </summary>
            BlinkOnReceive = 0x01,

            /// <summary>
            /// Indicator LED on USB-UIRT lights during IR transmission.
            /// </summary>
            BlinkOnTransmit = 0x02,

            /// <summary>
            /// Generate 'legacy' UIRT-compatible codes on receive
            /// </summary>
            GenerateLegacyCodesOnReceive = 0x04,

            /// <summary>
            /// Reserved
            /// </summary>
            Reserved0 = 0x08,

            /// <summary>
            /// Reserved
            /// </summary>
            Reserved1 = 0x10
        }

        /// <summary>
        /// Retrieves the current feature configuration bits from the USB-UIRT's nonvolatile 
        /// configuration memory. These various configuration bits control how the USB-UIRT 
        /// behaves. Most are reserved for future implementation and should be read and 
        /// written as Zero.
        /// </summary>
        /// <param name="hDrvHandle">Handle to to USB-UIRT returned by UUIRTOpen</param>
        /// <param name="uConfig">Integer representation of USB-UIRT configuration</param>
        /// <returns>TRUE on success</returns>
        /// <remarks> Using this API call is optional and is only needed to support 
        /// changing USB-UIRT's private preferences</remarks>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern bool UUIRTGetUUIRTConfig(IntPtr hDrvHandle, out uint uConfig);

        /// <summary>
        /// Configures the current feature configuration bits for the USB-UIRT's nonvolatile 
        /// configuration memory. These various configuration bits control how the USB-UIRT 
        /// behaves.
        /// </summary>
        /// <param name="hDrvHandle">Handle to to USB-UIRT returned by UUIRTOpen</param>
        /// <param name="uConfig">Integer representation of USB-UIRT configuration</param>
        /// <returns>TRUE on success</returns>
        [DllImport("uuirtdrv.dll", SetLastError = true)]
        private static extern bool UUIRTSetUUIRTConfig(IntPtr hDrvHandle, uint uConfig);
    }
}