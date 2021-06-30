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
        public uint Timeout { get; set; }
        /// <summary>
        /// Reconnecting count when disconnected.
        /// </summary>
        public ushort ReconnectCount { get; set; }
    }
}
