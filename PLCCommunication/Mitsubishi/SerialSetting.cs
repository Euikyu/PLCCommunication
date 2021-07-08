using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Mitsubishi
{
    public class SerialSetting
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public Handshake Handshake { get; set; }
        /// <summary>
        /// Initialized PC code in PLC Serial port. (Default value is 0xFF)
        /// </summary>
        public byte PCNo { get; set; }
        /// <summary>
        /// Initialized Network code in PLC Serial port. (Default value is 0x00)
        /// </summary>
        public byte NetworkNo { get; set; }
        public byte HostStationNo { get; set; }
        /// <summary>
        /// Reconnecting count when disconnected.
        /// </summary>
        public ushort ReconnectCount { get; set; }
    }
}
