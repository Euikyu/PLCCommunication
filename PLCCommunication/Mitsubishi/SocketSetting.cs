using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Mitsubishi
{
    /// <summary>
    /// Class related TCP/IP communication arguments.
    /// </summary>
    public class SocketSetting
    {
        #region Fields
        private IPAddress m_IPAddress;
        private string m_IP;
        #endregion

        #region Properties
        /// <summary>
        /// Destination IP of PLC to connect.
        /// </summary>
        public string IP
        {
            get { return m_IP; }
            set
            {
                var strBuf = value.Split('.');
                if (strBuf.Length == 4)
                {
                    byte[] tmpIP = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        if (!byte.TryParse(strBuf[i], out byte tmpAddress))
                        {
                            return;
                        }
                        tmpIP[i] = tmpAddress;
                    }
                    m_IP = value;
                    m_IPAddress = new IPAddress(tmpIP);
                }
            }
        }
        /// <summary>
        /// Destination port number of PLC to connect.
        /// </summary>
        public int PortNumber { get; set; }
        /// <summary>
        /// Initialized PC code in PLC LAN port. (Default value is 0xFF)
        /// </summary>
        public byte PCNo { get; set; }
        /// <summary>
        /// Initialized Network code in PLC LAN port. (Default value is 0x00)
        /// </summary>
        public byte NetworkNo { get; set; }
        /// <summary>
        /// Initialized protocol format in PLC.
        /// </summary>
        public EPLCProtocolFormat ProtocolFormat { get; set; }
        /// <summary>
        /// Waiting time for reply.
        /// </summary>
        public uint Timeout { get; set; }
        /// <summary>
        /// Reconnecting count when disconnected.
        /// </summary>
        public ushort ReconnectCount { get; set; }
        #endregion

        /// <summary>
        /// Generate socket communication setting instance.
        /// </summary>
        public SocketSetting() { }

        #region Methods
        /// <summary>
        /// Get IPAddress instance from string IP address.
        /// </summary>
        /// <returns>Current IP Address instance.</returns>
        public IPAddress GetIPAddress()
        {
            return m_IPAddress;
        }
        #endregion

    }
}
