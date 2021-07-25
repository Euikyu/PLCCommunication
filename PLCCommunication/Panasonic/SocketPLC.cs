using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PLCCommunication.Panasonic
{
    /// <summary>
    /// Class related a TCP/IP socket communication on PLC.
    /// </summary>
    public class SocketPLC : IPLC
    {
        #region Fields
        private readonly string CR = Encoding.ASCII.GetString(new byte[] { 0x0D });
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
        /// Initialized PLC unit number in PLC LAN port. (Default value is 0x01)
        /// </summary>
        public byte UnitNo
        {
            get { return m_Setting == null ? byte.MinValue : m_Setting.UnitNo; }
            set { if (m_Setting != null) m_Setting.UnitNo = value; }
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
        /// Create socket communication class on default value.
        /// </summary>
        public SocketPLC()
        {
            m_Setting = new SocketSetting();

            this.IP = "192.168.10.100";
            this.PortNumber = 6000;
            this.ProtocolFormat = EPLCProtocolFormat.Binary;
            this.UnitNo = 1;
            this.Timeout = 1000;
        }

        /// <summary>
        /// Create socket communication class on specific values.
        /// </summary>
        /// <param name="ipAddress">IP to connect.</param>
        /// <param name="portNum">Port number to connect.</param>
        /// <param name="protocolFormat">Protocol format in PLC to connect.</param>
        /// <param name="unitNo">Unit number of PLC to send.</param>
        /// <param name="timeout">Connection max delay value.</param>
        public SocketPLC(string ipAddress, int portNum, EPLCProtocolFormat protocolFormat = EPLCProtocolFormat.Binary, byte unitNo = 0x01, uint timeout = 4000)
        {
            m_Setting = new SocketSetting();

            this.IP = ipAddress;
            this.PortNumber = portNum;
            this.ProtocolFormat = protocolFormat;
            this.UnitNo = unitNo;
            this.Timeout = timeout;
        }

        public void Dispose()
        {
            this.Disconnect();
        }


        #region Methods

        /// <summary>
        /// Connect PLC to current IP & port number.
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
        /// Connect PLC to specific IP & port number.
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

            if (m_ReadThread == null || !m_ReadThread.IsAlive)
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
            if (m_ConnectionCheckThread != null && m_ConnectionCheckThread.IsAlive)
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
            if (m_ReadThread != null && m_ReadThread.IsAlive)
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
                        catch (ThreadAbortException)
                        {
                            throw;
                        }
                        catch (Exception err)
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
        public void Load()
        {
            if (!File.Exists(_DefaultPath + @"\Panasonic_Socket.xml"))
            {
                Save();
            }
            XmlSerializer serializer = new XmlSerializer(typeof(SocketSetting));
            using (var sr = new StreamReader(_DefaultPath + @"\Panasonic_Socket.xml"))
            {
                m_Setting = serializer.Deserialize(sr) as SocketSetting ?? m_Setting;
            }
        }

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

        public void Save()
        {
            Directory.CreateDirectory(_DefaultPath);
            XmlSerializer serializer = new XmlSerializer(typeof(SocketSetting));
            using (var sw = new StreamWriter(_DefaultPath + @"\Panasonic_Socket.xml"))
            {
                serializer.Serialize(sw, m_Setting ?? new SocketSetting());
            }
        }

        public void Save(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
            {
                var tmpArr = filePath.Split('\\');
                var dirPath = string.Empty;

                for (int i = 0; i < tmpArr.Length - 1; i++)
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

        #region Communicate Methods
        private void SendMsg(PLCSendingPacket data)
        {
            byte[] sendMsg = null;
            Type dataType = data.Value.GetType();
            switch (ProtocolFormat)
            {
                case EPLCProtocolFormat.ASCII:
                    string strHeader = "%" + UnitNo.ToString("00") + "#W";
                    string strData = string.Empty;

                    if ((int)data.DeviceCode < (int)EPLCDeviceCode.T) throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
                    else if ((int)data.DeviceCode <= (int)EPLCDeviceCode.C_L)
                    {
                        strHeader += "C";

                        if (dataType == typeof(bool))
                        {
                            strData += "S" + PLCConverter.ConvertStringFromAddress(data) + PLCConverter.Convert1BitStringFromBooleanData(data.Value is bool b ? b : false);
                        }
                        else
                        {
                            strData += "C" + PLCConverter.ConvertStringFromAddress(data, PLCConverter.CalcWordCount(data.Value));
                            if (data.Value is IEnumerable<bool> boolArr)
                            {
                                strData += PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
                            }
                            else if (data.Value is IEnumerable itemList && !(dataType == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte>))
                            {
                                strData += PLCConverter.ConvertMultiWordsStringFromDataList(itemList);
                            }
                            else
                            {
                                strData += PLCConverter.ConvertMultiWordsStringFromData(data.Value);
                            }
                        }
                    }
                    else if ((int)data.DeviceCode <= (int)EPLCDeviceCode.F)
                    {
                        strHeader += "D";
                        strData += PLCConverter.ConvertStringFromAddress(data, PLCConverter.CalcWordCount(data.Value));
                        if (data.Value is IEnumerable<bool> boolArr)
                        {
                            strData += PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
                        }
                        else if (data.Value is IEnumerable itemList && !(dataType == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte>))
                        {
                            strData += PLCConverter.ConvertMultiWordsStringFromDataList(itemList);
                        }
                        else
                        {
                            strData += PLCConverter.ConvertMultiWordsStringFromData(data.Value);
                        }
                    }
                    else
                    {
                        strHeader += "D";
                        strData += PLCConverter.ConvertStringFromAddress(data);

                        var tmpData = string.Empty;
                        if (data.Value is IEnumerable<bool> boolArr)
                        {
                            tmpData += PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
                        }
                        else if (data.Value is IEnumerable itemList && !(dataType == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte>))
                        {
                            tmpData += PLCConverter.ConvertMultiWordsStringFromDataList(itemList);
                        }
                        else
                        {
                            tmpData += PLCConverter.ConvertMultiWordsStringFromData(data.Value);
                        }

                        if ((int)data.DeviceCode <= (int)EPLCDeviceCode.IY)
                        {
                            tmpData = tmpData.Length < 4 ? ("0000" + tmpData).Substring(tmpData.Length, 4) : tmpData.Substring(0, 4);
                            strData += tmpData; 
                        }
                        else
                        {
                            tmpData = tmpData.Length < 8 ? ("00000000" + tmpData).Substring(tmpData.Length, 8) : tmpData.Substring(0, 8);
                            strData += tmpData;
                        }
                    }
                    sendMsg = Encoding.ASCII.GetBytes(strHeader + strData + PLCConverter.EncodeBCC(strHeader + strData) + CR);
                    
                    break;

                case EPLCProtocolFormat.Binary:
                    
                    List<byte> binData = new List<byte> { 0x80 };

                    if (data.IsContact)
                    {
                        binData.Add(0x52);
                        binData.AddRange(PLCConverter.ConvertByteArrayFromContactAddress(data));
                        binData.Add(PLCConverter.Convert1BitByteFromBooleanData(data.Value is bool b ? b : false));
                    }
                    else
                    {
                        binData.Add(0x50);
                        binData.AddRange(PLCConverter.ConvertByteArrayFromDataAddress(data, PLCConverter.CalcWordCount(data.Value)));
                        if (data.Value is IEnumerable<bool> boolArr)
                        {
                            binData.AddRange(PLCConverter.ConvertNWordsByteArrayFromBooleanArrayData(boolArr));
                        }
                        else if (data.Value is IEnumerable itemList && !(dataType == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte>))
                        {
                            binData.AddRange(PLCConverter.ConvertMultiWordsByteArrayFromDataList(itemList));
                        }
                        else
                        {
                            binData.AddRange(PLCConverter.ConvertMultiWordsByteArrayFromData(data.Value));
                        }
                    }
                    sendMsg = binData.ToArray();
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

            switch (ProtocolFormat)
            {
                case EPLCProtocolFormat.ASCII:
                    string strHeader = "%" + UnitNo.ToString("00") + "#R";
                    string strData = string.Empty;
                    
                    if ((int)data.DeviceCode < (int)EPLCDeviceCode.T) throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
                    else if ((int)data.DeviceCode <= (int)EPLCDeviceCode.C_L)
                    {
                        strHeader += "C";
                        strData += "C" + PLCConverter.ConvertStringFromAddress(data, data.WordCount);
                    }
                    else if((int)data.DeviceCode <= (int)EPLCDeviceCode.F)
                    {
                        strHeader += "D";
                        strData += PLCConverter.ConvertStringFromAddress(data, data.WordCount);
                    }
                    else
                    {
                        strHeader += "D";
                        strData += PLCConverter.ConvertStringFromAddress(data);
                    }

                    sendMsg = Encoding.ASCII.GetBytes(strHeader + strData + PLCConverter.EncodeBCC(strHeader + strData) + CR);

                    break;

                case EPLCProtocolFormat.Binary:
                    List<byte> binData = new List<byte> { 0x80 };

                    if (data.IsContact)
                    {
                        binData.Add(0x53);
                        binData.AddRange(PLCConverter.ConvertByteArrayFromContactAddress(data));
                        binData.Add(0x00);
                    }
                    else
                    {
                        binData.Add(0x51);
                        binData.AddRange(PLCConverter.ConvertByteArrayFromDataAddress(data, data.WordCount));
                    }
                    sendMsg = binData.ToArray();
                    binData.Clear();
                    binData = null;

                    break;
            }

            lock (_CommunicationLock)
            {
                m_Stream.Write(sendMsg, 0, sendMsg.Length);

                var tmpArr = this.ReceiveMsg(data.WordCount);
                readVal = tmpArr;

                receiveData = new PLCReceivingPacket(readVal, data.DeviceCode, data.ContactAddress);
            }
        }

        private byte[] ReceiveMsg(ushort wordCount = 0)
        {
            string receiveData = string.Empty;
            string tmpStr = string.Empty;
            byte[] resByteArr = null;

            while (m_CurrentData == null)
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
                        if (receiveData.Contains("!")) throw new Exception("Received error message." + Environment.NewLine + "Message : " + receiveData.Substring(4, 2));
                        if (!PLCConverter.DecodeBCC(receiveData.Substring(0, receiveData.Length - 3), receiveData.Substring(receiveData.Length - 3, 2))) throw new Exception("Received wrong message. (Different BCC.)" + Environment.NewLine + "Message : " + receiveData);

                        if (receiveData.Contains("%FF") || receiveData.Contains("<FF")) return new byte[0];
                        if (receiveData.Contains("$R"))
                        {
                            resByteArr = PLCConverter.ConvertHexStringToByteArray(receiveData.Substring(6, receiveData.Length - 3));
                            if(wordCount != resByteArr.Length / 2) throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + receiveData);
                            return resByteArr;
                        }
                        else if(receiveData.Contains("$W"))
                        {
                            return new byte[0];
                        }
                        else
                        {
                            throw new Exception("Received wrong message. (Unsupported format.)" + Environment.NewLine + "Message : " + receiveData);
                        }

                    case EPLCProtocolFormat.Binary:
                        if(m_CurrentData.Length < 3) throw new Exception("Received wrong message. (Wrong message type.)" + Environment.NewLine + "Message : " + BitConverter.ToString(m_CurrentData));
                        if(m_CurrentData[2] != 0xFF) throw new Exception("Received error message." + Environment.NewLine + "Error code : " + m_CurrentData[2].ToString("X2"));

                        resByteArr = m_CurrentData.Skip(3).ToArray();
                        if (wordCount != resByteArr.Length / 2) throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + BitConverter.ToString(m_CurrentData));

                        return resByteArr;
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
