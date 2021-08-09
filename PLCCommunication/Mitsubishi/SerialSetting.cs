using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Mitsubishi
{
    /// <summary>
    /// Class related to serial communication settings.
    /// </summary>
    public class SerialSetting
    {
        /// <summary>
        /// Destination serial port name to connect.
        /// </summary>
        public string PortName { get; set; }
        /// <summary>
        /// Destination baud rate to connect.
        /// </summary>
        public int BaudRate { get; set; }
        /// <summary>
        /// Data bits for transmit.
        /// </summary>
        public int DataBits { get; set; }
        /// <summary>
        /// Parity type to communicate.
        /// </summary>
        public Parity Parity { get; set; }
        /// <summary>
        /// Stop bits type to communicate.
        /// </summary>
        public StopBits StopBits { get; set; }
        /// <summary>
        /// Handshake type to communicate.
        /// </summary>
        public Handshake Handshake { get; set; }
        /// <summary>
        /// Initialized PC code in PLC. (Default value is 0xFF)
        /// </summary>
        public byte PCNo { get; set; }
        /// <summary>
        /// Initialized network code in PLC. (Default value is 0x00)
        /// </summary>
        public byte NetworkNo { get; set; }
        /// <summary>
        /// Initialized host station code in PLC. (Default value is 0x00)
        /// </summary>
        public byte HostStationNo { get; set; }
        /// <summary>
        /// Reconnecting count when disconnected.
        /// </summary>
        public ushort ReconnectCount { get; set; }

        /// <summary>
        /// Generate serial setting instance.
        /// </summary>
        public SerialSetting() { }
    }
}
