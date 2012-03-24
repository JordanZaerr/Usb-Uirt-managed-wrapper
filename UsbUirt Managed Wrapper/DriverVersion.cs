using System;

namespace UsbUirt
{
    public class DriverVersion
    {
        public DriverVersion(uint driverVersion, DateTime firmwareDate, uint firmwareVersion, uint protocolVersion)
        {
            FirmwareDate = firmwareDate;
            FirmwareVersion = firmwareVersion;
            ProtocolVersion = protocolVersion;
            Version = driverVersion;
        }

        public DateTime FirmwareDate { get; private set; }
        public uint FirmwareVersion { get; private set; }
        public uint ProtocolVersion { get; private set; }
        public uint Version { get; private set; }

        public override string ToString()
        {
            return string.Format("Driver Version: {0}\r\nFirmware Date: {1}\r\nFirmware Version: {2}\r\nProtocol Version: {3}",
                Version, FirmwareDate, FirmwareVersion, ProtocolVersion);
        }
    }
}