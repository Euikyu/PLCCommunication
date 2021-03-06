using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PLCCommunication.Mitsubishi
{
    /// <summary>
    /// Class related a TCP/IP socket communication of PLC.
    /// </summary>
    public class SocketPLC : IPLC
    {
        #region Fields
        private readonly string _DefaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Check Box";
        private readonly object _CommunicationLock = new object();
        private TcpClient m_Client;
        private NetworkStream m_Stream;

        private SocketSetting m_Setting;
        
        private Thread m_ConnectionCheckThread;
        private Thread m_ReadThread;

        private byte[] m_CurrentData;
        #endregion


        #region Properties
        /// <summary>
        /// Destination IP of PLC to connect.
        /// </summary>
        public string IP
        {
            get { return (m_Setting != null) ? m_Setting.IP : string.Empty; }
            set { if (m_Setting != null) m_Setting.IP = value; }
        }
        /// <summary>
        /// Destination port number of PLC to connect.
        /// </summary>
        public int PortNumber
        {
            get { return m_Setting == null ? 0 : m_Setting.PortNumber; }
            set { if (m_Setting != null) m_Setting.PortNumber = value; }
        }
        /// <summary>
        /// Initialized PC code in PLC. (Default value is 0xFF)
        /// </summary>
        public byte PCNo
        {
            get { return m_Setting == null ? byte.MaxValue : m_Setting.PCNo; }
            set { if (m_Setting != null) m_Setting.PCNo = value; }
        }
        /// <summary>
        /// Initialized network code in PLC. (Default value is 0x00)
        /// </summary>
        public byte NetworkNo
        {
            get { return m_Setting == null ? byte.MinValue : m_Setting.NetworkNo; }
            set { if (m_Setting != null) m_Setting.NetworkNo = value; }
        }
        /// <summary>
        /// Initialized protocol format in PLC.
        /// </summary>
        public EPLCProtocolFormat ProtocolFormat
        {
            get { return m_Setting == null ? EPLCProtocolFormat.Binary : m_Setting.ProtocolFormat; }
            set { if (m_Setting != null) m_Setting.ProtocolFormat = value; }
        }
        /// <summary>
        /// Waiting time for reply.
        /// </summary>
        public uint Timeout
        {
            get { return m_Setting == null ? uint.MinValue : m_Setting.Timeout * 250; }
            set { if (m_Setting != null) m_Setting.Timeout = value / 250; }
        }
        /// <summary>
        /// Reconnecting count when disconnected.
        /// (Period is 100ms and 0 is continuous.)
        /// </summary>
        public ushort ReconnectCount
        {
            get { return m_Setting == null ? ushort.MinValue : m_Setting.ReconnectCount; }
            set { if (m_Setting != null) m_Setting.ReconnectCount = value; }
        }
        /// <summary>
        /// Define that PLC is successfully communicating PC.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return !this.IP.Equals(string.Empty) && m_Client != null && m_Client.Connected;
            }
        }
        #endregion

        /// <summary>
        /// Generate socket communication instance with default value.
        /// </summary>
        public SocketPLC()
        {
            m_Setting = new SocketSetting();

            this.IP = "192.168.10.100";
            this.PortNumber = 6000;
            this.ProtocolFormat = EPLCProtocolFormat.Binary;
            this.NetworkNo = 0x00;
            this.PCNo = 0xFF;
            this.Timeout = 1000;
        }

        /// <summary>
        /// Generate socket communication instance with specific values.
        /// </summary>
        /// <param name="ipAddress">IP to connect.</param>
        /// <param name="portNum">Port number to connect.</param>
        /// <param name="protocolFormat">Protocol format in PLC to connect.</param>
        /// <param name="networkNo">Network code in PLC LAN port to connect.</param>
        /// <param name="pcNo">PC code in PLC LAN port to connect.</param>
        /// <param name="timeout">Connection max delay value.</param>
        public SocketPLC(string ipAddress, int portNum, EPLCProtocolFormat protocolFormat = EPLCProtocolFormat.Binary, byte networkNo = 0x00, byte pcNo = 0xFF, uint timeout = 4000)
        {
            m_Setting = new SocketSetting();

            this.IP = ipAddress;
            this.PortNumber = portNum;
            this.ProtocolFormat = protocolFormat;
            this.NetworkNo = networkNo;
            this.PCNo = pcNo;
            this.Timeout = timeout;
        }


#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        public void Dispose()
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        {
            this.Disconnect();
        }


        #region Methods

        /// <summary>
        /// Connect PLC to current IP &amp; port number.
        /// </summary>
        /// <returns></returns>
        public void Connect()
        {
            if (IsConnected) throw new Exception("Already connected.");
            this.TcpConnect();
            m_ConnectionCheckThread = new Thread(new ThreadStart(OnCheckProcess))
            {
                Name = "Socket_ConnectionCheck"
            };
            m_ConnectionCheckThread.Start();
        }

        /// <summary>
        /// Connect PLC to specific IP &amp; port number.
        /// </summary>
        /// <param name="ip">IP to connect newly.</param>
        /// <param name="portNum">Port number to connect newly.</param>
        /// <returns></returns>
        public void Connect(string ip, int portNum)
        {
            this.IP = ip;
            this.PortNumber = portNum;
            this.Connect();
        }

        /// <summary>
        /// Connect TCP/IP communication.
        /// </summary>
        private void TcpConnect()
        {
            m_Client = new TcpClient();
            m_Client.Connect(IP, PortNumber);
            m_Stream = m_Client.GetStream();

            if(m_ReadThread == null || !m_ReadThread.IsAlive)
            {
                m_ReadThread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        byte[] tmpBuffer = new byte[256];
                        int len = 0;
                        while ((len = m_Stream.Read(tmpBuffer, 0, tmpBuffer.Length)) != 0)
                        {
                            if (len == tmpBuffer.Length)
                            {
                                List<byte> combinedBufferList = new List<byte>(tmpBuffer);
                                while (len == tmpBuffer.Length)
                                {
                                    len = m_Stream.Read(tmpBuffer, 0, tmpBuffer.Length);
                                    combinedBufferList.AddRange(tmpBuffer.Take(len));
                                }
                                m_CurrentData = combinedBufferList.ToArray();
                                combinedBufferList.Clear();
                                combinedBufferList = null;
                            }
                            else m_CurrentData = tmpBuffer.Take(len).ToArray();

                            tmpBuffer = new byte[256];
                            len = 0;
                        }
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception err)
                    {
                        System.Windows.MessageBox.Show(err.Message);
                    }
                    finally
                    {
                        m_CurrentData = new byte[] { };

                        if (m_Stream != null)
                        {
                            m_Stream.Flush();
                            m_Stream.Close(500);
                            m_Stream = null;
                        }

                        if (m_Client != null)
                        {
                            m_Client.Close();
                            m_Client = null;
                        }
                    }
                }))
                {
                    Name = "Socket_DataRead"
                };
                m_ReadThread.Start();
            }
        }

        /// <summary>
        /// Disconnect this communication.
        /// </summary>
        /// <returns></returns>
        public void Disconnect()
        {
            if(m_ConnectionCheckThread!= null && m_ConnectionCheckThread.IsAlive)
            {
                m_ConnectionCheckThread.Abort();
                m_ConnectionCheckThread.Join(1000);
            }
            this.TcpDisconnect();
        }

        /// <summary>
        /// Disconnect TCP/IP communication.
        /// </summary>
        /// <returns></returns>
        private void TcpDisconnect()
        {
            if(m_ReadThread != null && m_ReadThread.IsAlive)
            {
                m_ReadThread.Abort();
                m_ReadThread.Join(1000);
            }
            if (m_Stream != null)
            {
                m_Stream.Flush();
                m_Stream.Close(500);
                m_Stream = null;
            }

            if (m_Client != null)
            {
                m_Client.Close();
                m_Client = null;
            }
        }

        /// <summary>
        /// Refresh connection state.
        /// </summary>
        /// <returns></returns>
        public void Refresh()
        {
            this.TcpDisconnect();
            this.TcpConnect();
        }

        private void OnCheckProcess()
        {
            try
            {
                int count = 0;
                while (true)
                {
                    //it change false only stream.Read() or stream.Write() is failed.
                    if (!IsConnected)
                    {
                        //retry communicating
                        try
                        {
                            this.Refresh();
                        }
                        catch(ThreadAbortException)
                        {
                            throw;
                        }
                        catch(Exception err)
                        {
                            if (this.ReconnectCount == 0) continue;

                            if (count > ReconnectCount) throw new Exception("PLC reconnection failed : " + err.Message + Environment.NewLine + "Please check LAN cable or PLC power.");
                            count++;
                            Thread.Sleep(100);
                            continue;
                        }
                        count = 0;
                    }
                    //Duration 1 sec
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception err)
            {
                System.Windows.MessageBox.Show(err.Message);
            }
        }

        /// <summary>
        /// Load setting files with default path.
        /// </summary>
        public void Load()
        {
            if(!File.Exists(_DefaultPath + @"\Mitsubishi_Socket.xml"))
            {
                Save();
            }
            XmlSerializer serializer = new XmlSerializer(typeof(SocketSetting));
            using(var sr = new StreamReader(_DefaultPath + @"\Mitsubishi_Socket.xml"))
            { 
                m_Setting = serializer.Deserialize(sr) as SocketSetting ?? m_Setting;
            }
        }

        /// <summary>
        /// Load setting files with specific path.
        /// </summary>
        /// <param name="filePath">File path to load.</param>
        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Save(filePath);
            }
            XmlSerializer serializer = new XmlSerializer(typeof(SocketSetting));
            using (var sr = new StreamReader(filePath))
            {
                m_Setting = serializer.Deserialize(sr) as SocketSetting ?? m_Setting;
            }
        }
        /// <summary>
        /// Save setting files with default path.
        /// </summary>
        public void Save()
        {
            Directory.CreateDirectory(_DefaultPath);
            XmlSerializer serializer = new XmlSerializer(typeof(SocketSetting));
            using (var sw = new StreamWriter(_DefaultPath + @"\Mitsubishi_Socket.xml"))
            {
                serializer.Serialize(sw, m_Setting ?? new SocketSetting());
            }
        }
        /// <summary>
        /// Save setting files with specific path.
        /// </summary>
        /// <param name="filePath">Path to save.</param>
        public void Save(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
            {
                var tmpArr = filePath.Split('\\');
                var dirPath = string.Empty;

                for(int i = 0; i < tmpArr.Length - 1; i++)
                {
                    dirPath += tmpArr[i] + "\\";
                }
                Directory.CreateDirectory(dirPath);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(SocketSetting));
            using (var sw = new StreamWriter(filePath))
            {
                serializer.Serialize(sw, m_Setting ?? new SocketSetting());
            }
        }

        /// <summary>
        /// Send message that try to write data having one address on PLC.
        /// </summary>
        /// <param name="data">A address data to write.</param>
        /// <returns></returns>
        public void Write(PLCSendingPacket data)
        {
            if (!IsConnected) throw new Exception("PLC not opened.");
            if (data.IsRead) throw new Exception("Wrong PLC massage type : Must use write type message.");

            this.SendMsg(data);
        }
        /// <summary>
        /// Send message that try to write data having several addresses on PLC.
        /// </summary>
        /// <param name="dataArr">Address data list to write.</param>
        /// <returns></returns>
        public void Write(IEnumerable<PLCSendingPacket> dataArr)
        {
            if (!IsConnected) throw new Exception("PLC not opened.");
            if (dataArr.Any(data => data.IsRead)) throw new Exception("Wrong PLC massage type : Must use write type messages.");

            this.SendMsg(dataArr);
        }

        /// <summary>
        /// Send message that try to bring data having one addresses on PLC.
        /// </summary>
        /// <param name="data">A address data to bring.</param>
        /// <param name="receiveValue">Received value in PLCReceiveData instance.</param>
        /// <returns></returns>
        public void Read(PLCSendingPacket data, ref PLCReceivingPacket receiveValue)
        {
            if (!IsConnected) throw new Exception("PLC not opened.");
            if (!data.IsRead) throw new Exception("Wrong PLC massage type : Must use read type message.");

            this.SendMsg(data, ref receiveValue);
        }
        /// <summary>
        /// Send message that try to bring data having several addresses on PLC.
        /// </summary>
        /// <param name="dataArr">Address data list to bring.</param>
        /// <param name="receiveValueList">Received data list in byte array.</param>
        /// <returns></returns>
        public void Read(IEnumerable<PLCSendingPacket> dataArr, ref List<PLCReceivingPacket> receiveValueList)
        {
            if (!IsConnected) throw new Exception("PLC not opened.");
            if (dataArr.Any(data => !data.IsRead)) throw new Exception("Wrong PLC massage type : Must use read type messages.");

            this.SendMsg(dataArr, ref receiveValueList);
        }
        

        #region Communicate Methods
        private void SendMsg(PLCSendingPacket data)
        {
            byte[] sendMsg = null;
            ushort msgDataLength = 0;
            ushort devCount = 0;
            Type dataType = data.Value.GetType();

            switch (ProtocolFormat)
            {
                case EPLCProtocolFormat.ASCII:
                    string strHeader = "5000" + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00";
                    string strData = m_Setting.Timeout.ToString("X4") + "1401";
                    if (data.Value is IEnumerable<bool> || dataType == typeof(bool))
                    {
                        strData += "0001" + PLCConverter.ConvertStringFromAddress(data);
                        if (data.Value.GetType() == typeof(bool))
                        {
                            strData += "0001" + PLCConverter.Convert1BitStringFromBooleanData((bool)data.Value);
                        }
                        else
                        {
                            var sendData = PLCConverter.ConvertNBitStringFromBooleanArrayData(data.Value as IEnumerable<bool>);
                            devCount = (ushort)sendData.Length;
                            strData += devCount.ToString("X4") + sendData;
                        }

                        msgDataLength = (ushort)strData.Length;
                    }
                    else
                    {
                        strData += "0000" + PLCConverter.ConvertStringFromAddress(data);
                        string sendData = string.Empty;
                        if (data.Value is IEnumerable itemList && !(dataType == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte>)) sendData = PLCConverter.ConvertMultiWordsStringFromDataList(itemList);
                        else sendData = PLCConverter.ConvertMultiWordsStringFromData(data.Value);
                        devCount = (ushort)(sendData.Length / 4);
                        strData += devCount.ToString("X4") + sendData;

                        msgDataLength = (ushort)strData.Length;
                    }

                    sendMsg = Encoding.ASCII.GetBytes(strHeader + msgDataLength.ToString("X4") + strData);

                    break;

                case EPLCProtocolFormat.Binary:
                    List<byte> binHeader = new List<byte>() { 0x50, 0x00, NetworkNo, PCNo, 0xFF, 0x03, 0x00 };
                    List<byte> binData = new List<byte>(BitConverter.GetBytes(m_Setting.Timeout).Take(2)) { 0x01, 0x14 };

                    if (data.Value is IEnumerable<bool> || dataType == typeof(bool))
                    {
                        binData.Add(0x01);
                        binData.Add(0x00);
                        binData.AddRange(PLCConverter.ConvertByteArrayFromAddress(data));
                        if (data.Value.GetType() == typeof(bool))
                        {
                            binData.Add(0x01);
                            binData.Add(0x00);
                            binData.Add(PLCConverter.Convert1BitByteFromBooleanData((bool)data.Value));
                        }
                        else
                        {
                            var sendData = PLCConverter.ConvertNBitByteArrayFromBooleanArrayData(data.Value as IEnumerable<bool>);
                            devCount = (ushort)(data.Value as IEnumerable<bool>).Count();
                            binData.AddRange(BitConverter.GetBytes(devCount));
                            binData.AddRange(sendData);
                        }

                        msgDataLength = (ushort)binData.Count;
                    }
                    else
                    {
                        binData.Add(0x00);
                        binData.Add(0x00);
                        binData.AddRange(PLCConverter.ConvertByteArrayFromAddress(data));

                        byte[] sendData = null;
                        if (data.Value is IEnumerable itemList && !(dataType == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte>)) sendData = PLCConverter.ConvertMultiWordsByteArrayFromDataList(itemList);
                        else sendData = PLCConverter.ConvertMultiWordsByteArrayFromData(data.Value);
                        devCount = (ushort)(sendData.Length / 2);
                        binData.AddRange(BitConverter.GetBytes(devCount));
                        binData.AddRange(sendData);

                        msgDataLength = (ushort)binData.Count;
                    }
                    binHeader.AddRange(BitConverter.GetBytes(msgDataLength));
                    binHeader.AddRange(binData);
                    sendMsg = binHeader.ToArray();

                    binHeader.Clear();
                    binHeader = null;
                    binData.Clear();
                    binData = null;

                    break;
            }

            lock (_CommunicationLock)
            {
                m_Stream.Write(sendMsg, 0, sendMsg.Length);

                this.ReceiveMsg();
            }
        }
        private void SendMsg(PLCSendingPacket data, ref PLCReceivingPacket receiveData)
        {
            byte[] sendMsg = null;
            byte[] readVal = null;
            ushort msgDataLength = 0;

            switch (ProtocolFormat)
            {
                case EPLCProtocolFormat.ASCII:
                    string strHeader = "5000" + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00";
                    string strData = m_Setting.Timeout.ToString("X4") + "0401" + "0000" + PLCConverter.ConvertStringFromAddress(data) + data.WordCount.ToString("X4");
                    msgDataLength = (ushort)strData.Length;
                    sendMsg = Encoding.ASCII.GetBytes(strHeader + msgDataLength.ToString("X4") + strData);
                    break;

                case EPLCProtocolFormat.Binary:
                    List<byte> binHeader = new List<byte>() { 0x50, 0x00, NetworkNo, PCNo, 0xFF, 0x03, 0x00 };
                    List<byte> binData = new List<byte>(BitConverter.GetBytes(m_Setting.Timeout).Take(2)) { 0x01, 0x04, 0x00, 0x00 };
                    binData.AddRange(PLCConverter.ConvertByteArrayFromAddress(data));
                    binData.AddRange(BitConverter.GetBytes(data.WordCount));

                    msgDataLength = (ushort)binData.Count;

                    binHeader.AddRange(BitConverter.GetBytes(msgDataLength));
                    binHeader.AddRange(binData);
                    sendMsg = binHeader.ToArray();

                    binHeader.Clear();
                    binHeader = null;
                    binData.Clear();
                    binData = null;

                    break;
            }

            lock (_CommunicationLock)
            {
                m_Stream.Write(sendMsg, 0, sendMsg.Length);

                var tmpArr = this.ReceiveMsg(data.WordCount);

                switch (ProtocolFormat)
                {
                    case EPLCProtocolFormat.Binary:
                        readVal = tmpArr;
                        break;
                    case EPLCProtocolFormat.ASCII:
                        readVal = new byte[tmpArr.Length];
                        for (int i = 0; i < tmpArr.Length; i += 2)
                        {
                            if (i + 1 < tmpArr.Length)
                            {
                                readVal[i + 1] = tmpArr[i];
                                readVal[i] = tmpArr[i + 1];
                            }
                            else if (i + 1 == tmpArr.Length)
                            {
                                readVal[i] = tmpArr[i];
                            }
                        }
                        break;
                }
                receiveData = new PLCReceivingPacket(readVal, data.DeviceCode, data.Address);
            }
        }
        private void SendMsg(IEnumerable<PLCSendingPacket> dataList)
        {
            byte[] sendMsg = null;
            ushort msgDataLength = 0;

            string strHeader = string.Empty;
            string strData = string.Empty;
            List<byte> binHeader = null;
            List<byte> binData = null;

            var boolDataList = dataList.Where(item => item.Value is IEnumerable<bool> || item.Value is bool);
            var objDataList = dataList.Where(item => !(item.Value is IEnumerable<bool> || item.Value is bool));

            if (boolDataList.Count() > 0)
            {
                byte bitCount = 0;
                switch (ProtocolFormat)
                {
                    case EPLCProtocolFormat.ASCII:
                        strHeader = "5000" + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00";
                        strData = m_Setting.Timeout.ToString("X4") + "1402" + "0001";
                        string strAddress = string.Empty;

                        foreach (var boolData in boolDataList)
                        {
                            if (boolData.Value is IEnumerable<bool> bListVal)
                            {
                                int len = bListVal.Count();
                                for (int i = 0; i < len; i++)
                                {
                                    strAddress += PLCConverter.ConvertStringFromAddress(boolData, i) + (bListVal.ElementAt(i) ? "01" : "00");
                                    bitCount++;
                                }
                            }
                            else if (boolData.Value is bool bVal)
                            {
                                strAddress += PLCConverter.ConvertStringFromAddress(boolData) + (bVal ? "01" : "00");
                                bitCount++;
                            }
                        }

                        strData += bitCount.ToString("X2") + strAddress;
                        msgDataLength = (ushort)strData.Length;

                        sendMsg = Encoding.ASCII.GetBytes(strHeader + msgDataLength.ToString("X4") + strData);

                        break;
                    case EPLCProtocolFormat.Binary:
                        binHeader = new List<byte>() { 0x50, 0x00, NetworkNo, PCNo, 0xFF, 0x03, 0x00 };
                        binData = new List<byte>(BitConverter.GetBytes(m_Setting.Timeout).Take(2)) { 0x02, 0x14, 0x01, 0x00 };
                        List<byte> binAddress = new List<byte>();
                        foreach (var boolData in boolDataList)
                        {
                            if (boolData.Value is IEnumerable<bool> bListVal)
                            {
                                int len = bListVal.Count();
                                for (int i = 0; i < len; i++)
                                {
                                    binAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(boolData, i));
                                    binAddress.Add((byte)boolData.DeviceCode);
                                    binAddress.Add(bListVal.ElementAt(i) ? (byte)0x01 : (byte)0x00);
                                    bitCount++;
                                }
                            }
                            else if (boolData.Value is bool bVal)
                            {
                                binAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(boolData));
                                binAddress.Add((byte)boolData.DeviceCode);
                                binAddress.Add(bVal ? (byte)0x01 : (byte)0x00);
                                bitCount++;
                            }
                        }

                        binData.AddRange(binAddress);

                        msgDataLength = binData.Count % 2 == 0 ? (ushort)(binData.Count / 2) : (ushort)(binData.Count / 2 + 1);
                        binHeader.AddRange(BitConverter.GetBytes(msgDataLength));
                        binHeader.AddRange(binData);

                        sendMsg = binHeader.ToArray();

                        binHeader.Clear();
                        binData.Clear();
                        binAddress.Clear();
                        binHeader = null;
                        binData = null;
                        binAddress = null;

                        break;
                }

                lock (_CommunicationLock)
                {
                    m_Stream.Write(sendMsg, 0, sendMsg.Length);

                    this.ReceiveMsg();
                }
            }

            if (objDataList.Count() > 0)
            {
                byte wordCount = 0;
                byte dwordCount = 0;

                switch (ProtocolFormat)
                {
                    case EPLCProtocolFormat.ASCII:
                        strHeader = "5000" + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00";
                        strData = m_Setting.Timeout.ToString("X4") + "1402" + "0000";
                        string strWordAddress = string.Empty;
                        string strDWordAddress = string.Empty;

                        foreach (var data in objDataList)
                        {
                            string tmpData = string.Empty;

                            if (data.Value is IEnumerable vals)
                            {
                                int tmpCount = 0;
                                if (vals is IEnumerable<char> || vals is string)
                                {
                                    tmpData = PLCConverter.ConvertMultiWordsStringFromData(vals);
                                    while (tmpData.Length >= 8)
                                    {
                                        strDWordAddress += PLCConverter.ConvertStringFromAddress(data, tmpCount * 2) + tmpData.Substring(4, 4) + tmpData.Substring(0, 4);
                                        tmpData = tmpData.Substring(8);
                                        dwordCount++;
                                        tmpCount++;
                                    }
                                    if (tmpData.Length > 0)
                                    {
                                        strWordAddress += PLCConverter.ConvertStringFromAddress(data, tmpCount * 2) + tmpData;
                                        wordCount++;
                                    }
                                }
                                else
                                {
                                    foreach (var val in vals)
                                    {
                                        if (val.GetType() == typeof(long) || val.GetType() == typeof(ulong) || val.GetType() == typeof(double))
                                        {
                                            var tmpArr = PLCConverter.Convert2WordsStringFrom4WordsData(val);
                                            strDWordAddress += PLCConverter.ConvertStringFromAddress(data, tmpCount);
                                            strDWordAddress += tmpArr[0];
                                            tmpCount += 2;
                                            strDWordAddress += PLCConverter.ConvertStringFromAddress(data, tmpCount);
                                            strDWordAddress += tmpArr[1];
                                            tmpCount += 2;
                                            dwordCount += 2;
                                            continue;
                                        }
                                        try
                                        {
                                            tmpData = PLCConverter.Convert2WordsStringFromData(val);
                                            strDWordAddress += PLCConverter.ConvertStringFromAddress(data, tmpCount) + tmpData;
                                            tmpCount += 2;
                                            dwordCount++;
                                        }
                                        catch
                                        {
                                            tmpData = PLCConverter.Convert1WordStringFromData(val);
                                            strWordAddress += PLCConverter.ConvertStringFromAddress(data, tmpCount) + tmpData;
                                            tmpCount++;
                                            wordCount++;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (data.Value.GetType() == typeof(long) || data.Value.GetType() == typeof(ulong) || data.Value.GetType() == typeof(double))
                                {
                                    var tmpArr = PLCConverter.Convert2WordsStringFrom4WordsData(data.Value);
                                    strDWordAddress += PLCConverter.ConvertStringFromAddress(data) + tmpArr[0];
                                    strDWordAddress += PLCConverter.ConvertStringFromAddress(data, 2);
                                    strDWordAddress += tmpArr[1];
                                    dwordCount += 2;
                                    continue;
                                }
                                try
                                {
                                    tmpData = PLCConverter.Convert2WordsStringFromData(data.Value);
                                    strDWordAddress += PLCConverter.ConvertStringFromAddress(data) + tmpData;
                                    dwordCount++;
                                }
                                catch
                                {
                                    tmpData = PLCConverter.Convert1WordStringFromData(data.Value);
                                    strWordAddress += PLCConverter.ConvertStringFromAddress(data) + tmpData;
                                    wordCount++;
                                }
                            }
                        }

                        strData += wordCount.ToString("X2") + dwordCount.ToString("X2") + strWordAddress + strDWordAddress;
                        msgDataLength = (ushort)strData.Length;

                        sendMsg = Encoding.ASCII.GetBytes(strHeader + msgDataLength.ToString("X4") + strData);

                        break;

                    case EPLCProtocolFormat.Binary:
                        binHeader = new List<byte>() { 0x50, 0x00, NetworkNo, PCNo, 0xFF, 0x03, 0x00 };
                        binData = new List<byte>(BitConverter.GetBytes(m_Setting.Timeout)) { 0x02, 0x14, 0x00, 0x00 };
                        List<byte> binDWordAddress = new List<byte>();
                        List<byte> binWordAddress = new List<byte>();

                        foreach (var data in objDataList)
                        {
                            IEnumerable<byte> tmpData = null;

                            if (data.Value is IEnumerable vals)
                            {
                                int tmpCount = 0;
                                if (vals is IEnumerable<char> || vals is string)
                                {
                                    tmpData = PLCConverter.ConvertMultiWordsByteArrayFromData(vals);
                                    while (tmpData.Count() >= 4)
                                    {
                                        binDWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, tmpCount * 2));
                                        binDWordAddress.AddRange(tmpData.Take(4));
                                        tmpData = tmpData.Skip(4);
                                        dwordCount++;
                                        tmpCount++;
                                    }
                                    if (tmpData.Count() > 0)
                                    {
                                        binWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, tmpCount * 2));
                                        binWordAddress.AddRange(tmpData);
                                        wordCount++;
                                    }
                                }
                                else
                                {
                                    foreach (var val in vals)
                                    {
                                        if (val.GetType() == typeof(long) || val.GetType() == typeof(ulong) || val.GetType() == typeof(double))
                                        {
                                            var tmpArr = PLCConverter.Convert2WordsByteArrayFrom4WordsData(val);
                                            binDWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, tmpCount));
                                            binDWordAddress.AddRange(tmpArr[0]);
                                            tmpCount += 2;
                                            binDWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, tmpCount));
                                            binDWordAddress.AddRange(tmpArr[1]);
                                            tmpCount += 2;
                                            dwordCount += 2;
                                            continue;
                                        }
                                        else
                                        {
                                            try
                                            {
                                                tmpData = PLCConverter.Convert2WordsByteArrayFromData(val);
                                                binDWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, tmpCount));
                                                binDWordAddress.AddRange(tmpData);
                                                tmpCount += 2;
                                                dwordCount++;
                                            }
                                            catch
                                            {
                                                tmpData = PLCConverter.Convert1WordByteArrayFromData(val);
                                                binWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, tmpCount));
                                                binWordAddress.AddRange(tmpData);
                                                tmpCount++;
                                                wordCount++;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (data.Value.GetType() == typeof(long) || data.Value.GetType() == typeof(ulong) || data.Value.GetType() == typeof(double))
                                {
                                    var tmpArr = PLCConverter.Convert2WordsByteArrayFrom4WordsData(data.Value);
                                    binDWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data));
                                    binDWordAddress.AddRange(tmpArr[0]);
                                    binDWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, 2));
                                    binDWordAddress.AddRange(tmpArr[1]);
                                    dwordCount += 2;
                                    continue;
                                }
                                try
                                {
                                    tmpData = PLCConverter.Convert2WordsByteArrayFromData(data.Value);
                                    binDWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data));
                                    binDWordAddress.AddRange(tmpData);
                                    dwordCount++;
                                }
                                catch
                                {
                                    tmpData = PLCConverter.Convert1WordByteArrayFromData(data.Value);
                                    binWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data));
                                    binWordAddress.AddRange(tmpData);
                                    wordCount++;
                                }
                            }
                        }
                        binData.Add(wordCount);
                        binData.Add(dwordCount);
                        binData.AddRange(binWordAddress);
                        binData.AddRange(binDWordAddress);

                        msgDataLength = (ushort)binData.Count;


                        binHeader.AddRange(BitConverter.GetBytes(msgDataLength));
                        binHeader.AddRange(binData);

                        sendMsg = binHeader.ToArray();

                        binHeader.Clear();
                        binData.Clear();
                        binDWordAddress.Clear();
                        binWordAddress.Clear();
                        binHeader = null;
                        binData = null;
                        binDWordAddress = null;
                        binWordAddress = null;

                        break;
                }

                lock (_CommunicationLock)
                {
                    m_Stream.Write(sendMsg, 0, sendMsg.Length);

                    this.ReceiveMsg();
                }
            }
        }
        private void SendMsg(IEnumerable<PLCSendingPacket> dataList, ref List<PLCReceivingPacket> readValArr)
        {
            byte[] sendMsg = null;
            ushort msgDataLength = 0;

            string strHeader = string.Empty;
            string strData = string.Empty;
            List<byte> binHeader = null;
            List<byte> binData = null;
            byte wordCount = 0;
            byte dwordCount = 0;

            switch (ProtocolFormat)
            {
                case EPLCProtocolFormat.ASCII:
                    strHeader = "5000" + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00";
                    strData = m_Setting.Timeout.ToString("X4") + "0403" + "0000";
                    string strWordAddress = string.Empty;
                    string strDWordAddress = string.Empty;

                    foreach (var data in dataList)
                    {
                        var tmpDwordCount = data.WordCount / 2 + dwordCount > byte.MaxValue ? throw new Exception("Too much send messages at once.") : (byte)(data.WordCount / 2);
                        var tmpWordCount = data.WordCount % 2 + wordCount > byte.MaxValue ? throw new Exception("Too much send messages at once.") : (byte)(data.WordCount % 2);

                        for (int i = 0; i < tmpDwordCount; i++)
                        {
                            strDWordAddress += PLCConverter.ConvertStringFromAddress(data, 2 * i);
                        }
                        if (tmpWordCount > 0)
                        {
                            strWordAddress += PLCConverter.ConvertStringFromAddress(data, 2 * tmpDwordCount);
                        }
                        dwordCount += tmpDwordCount;
                        wordCount += tmpWordCount;
                    }

                    strData += wordCount.ToString("X2") + dwordCount.ToString("X2") + strWordAddress + strDWordAddress;
                    msgDataLength = (ushort)strData.Length;

                    sendMsg = Encoding.ASCII.GetBytes(strHeader + msgDataLength.ToString("X4") + strData);

                    break;

                case EPLCProtocolFormat.Binary:
                    binHeader = new List<byte>() { 0x50, 0x00, NetworkNo, PCNo, 0xFF, 0x03, 0x00 };
                    binData = new List<byte>(BitConverter.GetBytes(m_Setting.Timeout).Take(2)) { 0x03, 0x04, 0x00, 0x00 };
                    List<byte> binDWordAddress = new List<byte>();
                    List<byte> binWordAddress = new List<byte>();

                    foreach (var data in dataList)
                    {
                        var tmpDwordCount = data.WordCount / 2 + dwordCount > byte.MaxValue ? throw new Exception("Too much send messages at once.") : (byte)(data.WordCount / 2);
                        var tmpWordCount = data.WordCount % 2 + wordCount > byte.MaxValue ? throw new Exception("Too much send messages at once.") : (byte)(data.WordCount % 2);

                        for (int i = 0; i < tmpDwordCount; i++)
                        {
                            binDWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, 2 * i));
                        }
                        if (tmpWordCount > 0)
                        {
                            binWordAddress.AddRange(PLCConverter.ConvertByteArrayFromAddress(data, 2 * tmpDwordCount));
                        }
                        dwordCount += tmpDwordCount;
                        wordCount += tmpWordCount;
                    }

                    binData.Add(wordCount);
                    binData.Add(dwordCount);
                    binData.AddRange(binWordAddress);
                    binData.AddRange(binDWordAddress);

                    msgDataLength = (ushort)binData.Count;

                    binHeader.AddRange(BitConverter.GetBytes(msgDataLength));
                    binHeader.AddRange(binData);
                    sendMsg = binHeader.ToArray();

                    binHeader.Clear();
                    binData.Clear();
                    binDWordAddress.Clear();
                    binWordAddress.Clear();
                    binHeader = null;
                    binData = null;
                    binDWordAddress = null;
                    binWordAddress = null;
                    break;
            }

            lock (_CommunicationLock)
            {
                m_Stream.Write(sendMsg, 0, sendMsg.Length);

                var tmpArr = this.ReceiveMsg((ushort)(dwordCount * 2 + wordCount));

                var wordArr = tmpArr.Take(wordCount * 2);
                var dwordArr = tmpArr.Skip(wordCount * 2);

                List<PLCReceivingPacket> tmpResultList = new List<PLCReceivingPacket>();

                foreach (var data in dataList)
                {
                    List<byte> tmpList = new List<byte>();

                    var tmpDwordCount = data.WordCount / 2;
                    var tmpWordCount = data.WordCount % 2;

                    switch (ProtocolFormat)
                    {
                        case EPLCProtocolFormat.Binary:
                            if (tmpDwordCount > 0)
                            {
                                tmpList.AddRange(dwordArr.Take(tmpDwordCount * 4));
                                dwordArr = dwordArr.Skip(tmpDwordCount * 4);
                            }
                            if (tmpWordCount > 0)
                            {
                                tmpList.AddRange(wordArr.Take(tmpWordCount * 2));
                                wordArr = wordArr.Skip(tmpWordCount * 2);
                            }
                            if (tmpList.Count > 0)
                            {
                                tmpResultList.Add(new PLCReceivingPacket(tmpList.ToArray(), data.DeviceCode, data.Address));
                                tmpList.Clear();
                            }
                            tmpList = null;
                            break;
                        case EPLCProtocolFormat.ASCII:
                            if (tmpDwordCount > 0)
                            {
                                for (int i = 0; i < tmpDwordCount; i++)
                                {
                                    var tmpDwordArrForAscii = dwordArr.Skip(i * 4).Take(4);
                                    tmpList.AddRange(tmpDwordArrForAscii.Reverse());
                                }
                                dwordArr = dwordArr.Skip(tmpDwordCount * 4);
                            }
                            if (tmpWordCount > 0)
                            {
                                for (int i = 0; i < tmpWordCount; i++)
                                {
                                    var tmpWordArrForAscii = wordArr.Skip(i * 2).Take(2);
                                    tmpList.AddRange(tmpWordArrForAscii.Reverse());
                                }
                                wordArr = wordArr.Skip(tmpWordCount * 2);
                            }
                            if (tmpList.Count > 0)
                            {
                                tmpResultList.Add(new PLCReceivingPacket(tmpList.ToArray(), data.DeviceCode, data.Address));
                                tmpList.Clear();
                            }
                            tmpList = null;
                            break;

                    }
                }
                readValArr = tmpResultList;
            }
        }

        private byte[] ReceiveMsg(ushort wordCount = 0)
        {
            string receiveData = string.Empty;
            string tmpStr = string.Empty;

            while(m_CurrentData == null)
            {
                Thread.Sleep(10);
            }

            if (m_CurrentData.Length == 0)
            {
                m_CurrentData = null;
                throw new Exception("Session disconnected.");
            }
            try
            {
                switch (ProtocolFormat)
                {
                    case EPLCProtocolFormat.ASCII:
                        receiveData = Encoding.ASCII.GetString(m_CurrentData);
                        tmpStr = "D000" + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00";

                        if (!receiveData.Contains(tmpStr)) throw new Exception("Received wrong message. (Different header.)" + Environment.NewLine + "Message : " + receiveData);
                        int strCount = int.Parse(receiveData.Substring(tmpStr.Length, 4), System.Globalization.NumberStyles.HexNumber);
                        if (strCount != receiveData.Length - tmpStr.Length - 4 || strCount != wordCount * 4 + 4) throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + receiveData);

                        string errCode = receiveData.Substring(tmpStr.Length + 4, 4);
                        if (errCode != "0000") throw new Exception("Received error message." + Environment.NewLine + "Message : " + receiveData.Substring(tmpStr.Length + 8));
                        receiveData = receiveData.Substring(tmpStr.Length + 8);

                        return PLCConverter.ConvertHexStringToByteArray(receiveData);

                    case EPLCProtocolFormat.Binary:
                        var tmpByteArray = new byte[] { 0xD0, 0x00, NetworkNo, PCNo, 0xFF, 0x03, 0x00 };

                        for (int i = 0; i < tmpByteArray.Length; i++) if (tmpByteArray[i] != m_CurrentData[i]) throw new Exception("Received wrong message. (Different header.)" + Environment.NewLine + "Message : " + BitConverter.ToString(m_CurrentData));
                        ushort byteCount = BitConverter.ToUInt16(m_CurrentData.Skip(tmpByteArray.Length).Take(2).ToArray(), 0);
                        if (byteCount != m_CurrentData.Length - tmpByteArray.Length - 2 || byteCount != wordCount * 2 + 2) throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + BitConverter.ToString(m_CurrentData));
                        int errInt = BitConverter.ToUInt16(m_CurrentData.Skip(tmpByteArray.Length + 2).Take(2).ToArray(), 0);
                        if (errInt != 0) throw new Exception("Received error message." + Environment.NewLine + "Error code : " + PLCConverter.ConvertValueToString(m_CurrentData.Take(tmpByteArray.Length + 4).ToArray()));

                        return m_CurrentData.Skip(tmpByteArray.Length + 4).ToArray();
                }
                //can't arrive here
                throw new Exception();
            }
            catch
            {
                throw;
            }
            finally
            {
                m_CurrentData = null;
            }
        }
        #endregion
        
        #endregion
    }
}
