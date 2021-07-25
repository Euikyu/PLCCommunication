﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Panasonic
{
    public class SocketSetting
    {
        private IPAddress m_IPAddress;
        private string m_IP;

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
        /// Unit number of PLC to send.
        /// </summary>
        public byte UnitNo { get; set; }
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

        public IPAddress GetIPAddress()
        {
            return m_IPAddress;
        }
    }
}
