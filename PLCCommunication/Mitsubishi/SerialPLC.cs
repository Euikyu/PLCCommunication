using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PLCCommunication.Mitsubishi
{
    /// <summary>
    /// Class related to serial communication of PLC.
    /// </summary>
    public class SerialPLC : IPLC
    {
        #region Fields
        private const byte ENQ = 0x05;
        private const byte EOT = 0x04;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;
        private const byte CR = 0x0D;
        private const byte LF = 0x0A;
        private const byte CL = 0x0C;

        private readonly char[] END_OF_TRANSMISSION = new char[] { (char)EOT, (char)CR, (char)LF };
        private readonly char[] CLEAR_ALL_MESSAGES = new char[] { (char)CL, (char)CR, (char)LF };
        private readonly string PREFIX_STRING = new string(new char[] { (char)ENQ });
        private readonly string POSTFIX_STRING = new string(new char[] { (char)CR, (char)LF });

        private readonly string _DefaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Check Box";
        private readonly object _CommunicationLock = new object();
        private readonly object _SequenceLock = new object();
        private readonly Queue<AutoResetEvent> m_SequenceQueue = new Queue<AutoResetEvent>();
        private readonly ManualResetEvent m_TerminateEvent = new ManualResetEvent(false);

        private SerialPort m_Serial;
        private SerialSetting m_Setting;
        private Thread m_ConnectionCheckThread;


        private string m_StringBuffer = string.Empty;
        private string m_CurrentString = string.Empty;
        
        #endregion


        #region Properties
        /// <summary>
        /// Destination serial port name to connect.
        /// </summary>
        public string PortName
        {
            get { return m_Setting == null ? string.Empty : m_Setting.PortName; }
            set { if (m_Setting != null) m_Setting.PortName = value; }
        }
        /// <summary>
        /// Destination baud rate to connect.
        /// </summary>
        public int BaudRate
        {
            get { return m_Setting == null ? -1 : m_Setting.BaudRate; }
            set { if (m_Setting != null) m_Setting.BaudRate = value; }
        }
        /// <summary>
        /// Data bits for transmit.
        /// </summary>
        public int DataBits
        {
            get { return m_Setting == null ? -1 : m_Setting.DataBits; }
            set { if (m_Setting != null) m_Setting.DataBits = value; }
        }
        /// <summary>
        /// Parity type to communicate.
        /// </summary>
        public Parity Parity
        {
            get { return m_Setting == null ? Parity.None : m_Setting.Parity; }
            set { if (m_Setting != null) m_Setting.Parity = value; }
        }
        /// <summary>
        /// Stop bits type to communicate.
        /// </summary>
        public StopBits StopBits
        {
            get { return m_Setting == null ? StopBits.One : m_Setting.StopBits; }
            set { if (m_Setting != null) m_Setting.StopBits = value; }
        }
        /// <summary>
        /// Handshake type to communicate.
        /// </summary>
        public Handshake Handshake
        {
            get { return m_Setting == null ? Handshake.None : m_Setting.Handshake; }
            set { if (m_Setting != null) m_Setting.Handshake = value; }
        }
        /// <summary>
        /// Initialized host station code in PLC. (Default value is 0x00)
        /// </summary>
        public byte HostStationNo
        {
            get { return m_Setting == null ? byte.MinValue : m_Setting.HostStationNo; }
            set { if (m_Setting != null) m_Setting.HostStationNo = value; }
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
        /// Initialized PC code in PLC. (Default value is 0xFF)
        /// </summary>
        public byte PCNo
        {
            get { return m_Setting == null ? byte.MaxValue : m_Setting.PCNo; }
            set { if (m_Setting != null) m_Setting.PCNo = value; }
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
        /// (NULL is disconnected, and false is destination not ready.)
        /// </summary>
        public bool? IsConnected
        {
            get
            {
                if (m_Serial != null && m_Serial.IsOpen)
                {
                    return (m_Serial.Handshake == Handshake.None ? true : m_Serial.DsrHolding);
                }
                else return null;
            }
        }
        #endregion
        /// <summary>
        /// Generate serial communication instance with default value.
        /// </summary>
        public SerialPLC()
        {
            m_Setting = new SerialSetting();

            this.PortName = "COM1";
            this.BaudRate = 9600;
            this.DataBits = 8;
            this.Parity = Parity.None;
            this.StopBits = StopBits.One;
            this.Handshake = Handshake.None;
            this.HostStationNo = 0x00;
            this.NetworkNo = 0x00;
            this.PCNo = 0xFF;
        }
        /// <summary>
        /// Generate serial communication instance with specific values.
        /// </summary>
        /// <param name="portName">Serial port name to connect.</param>
        /// <param name="baudRate">Serial baud rate to connect.</param>
        /// <param name="dataBits">Data bits for transmit.</param>
        /// <param name="parity">Parity type to communicate.</param>
        /// <param name="stopBits">Stop bits type to communicate.</param>
        /// <param name="handshake">Handshake type to communicate.</param>
        /// <param name="hostStationNo">Host station number to communicate.</param>
        /// <param name="networkNo">Network number to communicate.</param>
        /// <param name="pcNo">PC number to communicate.</param>
        public SerialPLC(string portName, int baudRate, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, Handshake handshake = Handshake.None, byte hostStationNo = 0x00, byte networkNo = 0x00, byte pcNo = 0xFF)
        {
            m_Setting = new SerialSetting();

            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;
            this.Handshake = handshake;
            this.HostStationNo = hostStationNo;
            this.NetworkNo = networkNo;
            this.PCNo = pcNo;
        }

#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        public void Dispose()
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        {
            this.Disconnect();
            if(m_Serial != null) m_Serial.Dispose();
        }


        #region Methods
        /// <summary>
        /// Load setting files with default path.
        /// </summary>
        public void Load()
        {
            if (!File.Exists(_DefaultPath + @"\Mitsubishi_Serial.xml"))
            {
                this.Save();
            }
            XmlSerializer serializer = new XmlSerializer(typeof(SerialSetting));
            using (var sr = new StreamReader(_DefaultPath + @"\Mitsubishi_Serial.xml"))
            {
                m_Setting = serializer.Deserialize(sr) as SerialSetting ?? m_Setting;
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
                this.Save(filePath);
            }
            XmlSerializer serializer = new XmlSerializer(typeof(SerialSetting));
            using (var sr = new StreamReader(filePath))
            {
                m_Setting = serializer.Deserialize(sr) as SerialSetting ?? m_Setting;
            }
        }
        /// <summary>
        /// Save setting files with default path.
        /// </summary>
        public void Save()
        {
            Directory.CreateDirectory(_DefaultPath);
            XmlSerializer serializer = new XmlSerializer(typeof(SerialSetting));
            using (var sw = new StreamWriter(_DefaultPath + @"\Mitsubishi_Serial.xml"))
            {
                serializer.Serialize(sw, m_Setting ?? new SerialSetting());
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

                for (int i = 0; i < tmpArr.Length - 1; i++)
                {
                    dirPath += tmpArr[i] + "\\";
                }
                Directory.CreateDirectory(dirPath);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(SerialSetting));
            using (var sw = new StreamWriter(filePath))
            {
                serializer.Serialize(sw, m_Setting ?? new SerialSetting());
            }
        }
        /// <summary>
        /// Connect PLC.
        /// </summary>
        public void Connect()
        {
            if (IsConnected != null) throw new Exception("Already Opened.");

            this.SerialConnect();

            if(m_ConnectionCheckThread == null || !m_ConnectionCheckThread.IsAlive)
            {
                m_ConnectionCheckThread = new Thread(new ThreadStart(this.OnCheckProcess));
                m_ConnectionCheckThread.Start();
            }
        }
        /// <summary>
        /// Connect PLC with new Port name &amp; baud rate.
        /// </summary>
        /// <param name="portName">Serial port name to connect newly.</param>
        /// <param name="baudRate">Serial baud rate to connect newly.</param>
        public void Connect(string portName, int baudRate)
        {
            if (IsConnected != null) throw new Exception("Already Opened.");

            this.PortName = portName;
            this.BaudRate = baudRate;

            this.Connect();
        }
        /// <summary>
        /// Connect PLC with new several parameters.
        /// </summary>
        /// <param name="portName">Serial port name to connect newly.</param>
        /// <param name="baudRate">Serial baud rate to connect newly.</param>
        /// <param name="dataBits">Data bits for transmit newly.</param>
        /// <param name="parity">Parity type newly.</param>
        /// <param name="stopBits">Stop bits type newly.</param>
        /// <param name="handshake">Handshake type newly.</param>
        public void Connect(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, Handshake handshake)
        {
            if (IsConnected != null) throw new Exception("Already Opened.");

            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;
            this.Handshake = handshake;
            this.Connect();
        }

        /// <summary>
        /// Connect serial communication.
        /// </summary>
        private void SerialConnect()
        {
            m_Serial = new SerialPort()
            {
                PortName = this.PortName,
                BaudRate = this.BaudRate,
                Parity = this.Parity,
                DataBits = this.DataBits,
                StopBits = this.StopBits,
                Handshake = this.Handshake
            };
            m_Serial.DataReceived += Serial_DataReceived;
            m_Serial.Open();
            m_Serial.DtrEnable = true;
            m_TerminateEvent.Reset();
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            m_StringBuffer += m_Serial.ReadExisting();
            int len = -1;
            while ((len = m_StringBuffer.IndexOf(POSTFIX_STRING)) != -1)
            {
                len = len + POSTFIX_STRING.Length > m_StringBuffer.Length ? m_StringBuffer.Length : len + POSTFIX_STRING.Length;

                lock (_CommunicationLock)
                {
                    m_CurrentString = m_StringBuffer.Substring(0, len);
                    m_StringBuffer = m_StringBuffer.Remove(0, len);

                    if (m_CurrentString.SequenceEqual(END_OF_TRANSMISSION))
                    {
                        m_CurrentString = string.Empty;
                        m_StringBuffer = string.Empty;
                        m_Serial.Dispose();
                        return;
                    }
                    else if (m_CurrentString.SequenceEqual(CLEAR_ALL_MESSAGES))
                    {
                        m_CurrentString = string.Empty;
                        m_StringBuffer = string.Empty;
                        this.ClearMessages();
                        return;
                    }
                }
                lock (_SequenceLock)
                {
                    if (m_SequenceQueue.Count > 0) m_SequenceQueue.Dequeue().Set();
                }
            }
        }

        private void OnCheckProcess()
        {
            try
            {
                int count = 0;
                while (true)
                {
                    //it change false only stream.Read() or stream.Write() is failed.
                    if (!IsConnected.HasValue || !IsConnected.Value)
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
                            if (this.ReconnectCount == 0)
                            {
                                Thread.Sleep(100);
                                continue;
                            }
                            if (count > ReconnectCount) throw new Exception("PLC reconnection failed : " + err.Message + Environment.NewLine + "Please check serial cable or PLC power.");
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
        /// Disconnect PLC.
        /// </summary>
        public void Disconnect()
        {
            if(m_ConnectionCheckThread != null && m_ConnectionCheckThread.IsAlive)
            {
                m_ConnectionCheckThread.Abort();
                m_ConnectionCheckThread.Join(1000);
            }
            this.SerialDisconnect();
        }
        /// <summary>
        /// Flush this stream.
        /// </summary>
        private void ClearMessages()
        {
            lock(_SequenceLock) if (m_SequenceQueue != null) m_SequenceQueue.Clear();
            if(m_Serial != null && m_Serial.IsOpen)
            {
                m_Serial.DiscardInBuffer();
                m_Serial.DiscardOutBuffer();
            }
            m_TerminateEvent.Set();
        }
        /// <summary>
        /// Disconnect serial communication.
        /// </summary>
        private void SerialDisconnect()
        {
            this.ClearMessages();
            if(m_Serial != null)
            {
                m_Serial.DataReceived -= this.Serial_DataReceived;
                m_Serial.Close();
            }
        }

        /// <summary>
        /// Refresh current connection state.
        /// </summary>
        public void Refresh()
        {
            this.SerialDisconnect();
            this.SerialConnect();
        }

        /// <summary>
        /// Send message that try to write data having one address on PLC.
        /// </summary>
        /// <param name="data">A address data to write.</param>
        /// <returns></returns>
        public void Write(PLCSendingPacket data)
        {
            if (!IsConnected.HasValue || !IsConnected.Value) throw new Exception("PLC disconnected.");
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
            if (!IsConnected.HasValue || !IsConnected.Value) throw new Exception("PLC disconnected.");
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
            if (!IsConnected.HasValue || !IsConnected.Value) throw new Exception("PLC disconnected.");
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
            if (!IsConnected.HasValue || !IsConnected.Value) throw new Exception("PLC disconnected.");
            if (dataArr.Any(data => !data.IsRead)) throw new Exception("Wrong PLC massage type : Must use read type messages.");

            this.SendMsg(dataArr, ref receiveValueList);
        }


        #region Communicate Methods
        private void SendMsg(PLCSendingPacket data)
        {
            string sendMsg = string.Empty;
            ushort devCount = 0;
            Type dataType = data.Value.GetType();
            
            string strHeader = "F8" + HostStationNo.ToString("X2") + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00" + "00";
            string strData = "1401";
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
            }
            else
            {
                strData += "0000" + PLCConverter.ConvertStringFromAddress(data);
                string sendData = string.Empty;
                if (data.Value is IEnumerable itemList && !(dataType == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte>)) sendData = PLCConverter.ConvertMultiWordsStringFromDataList(itemList);
                else sendData = PLCConverter.ConvertMultiWordsStringFromData(data.Value);
                devCount = (ushort)(sendData.Length / 4);
                strData += devCount.ToString("X4") + sendData;
            }
            var checksumMsg = this.CalculateCheckSum(strHeader + strData);
            sendMsg = PREFIX_STRING + strHeader + strData + checksumMsg + POSTFIX_STRING;

            AutoResetEvent executeEvent = null;
            lock (_CommunicationLock)
            {
                m_Serial.Write(sendMsg);
                lock (_SequenceLock)
                {
                    executeEvent = new AutoResetEvent(false);
                    m_SequenceQueue.Enqueue(executeEvent);
                }
            }
            this.ReceiveMsg(executeEvent);
        }
        private void SendMsg(PLCSendingPacket data, ref PLCReceivingPacket receiveData)
        {
            string sendMsg = string.Empty;
            byte[] readVal = null;

            string strHeader = "F8" + HostStationNo.ToString("X2") + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00" + "00";
            string strData = "0401" + "0000" + PLCConverter.ConvertStringFromAddress(data) + data.WordCount.ToString("X4");
            var checksumMsg = this.CalculateCheckSum(strHeader + strData);           
            sendMsg = PREFIX_STRING + strHeader + strData + checksumMsg + POSTFIX_STRING;

            AutoResetEvent executeEvent = null;
            lock (_CommunicationLock)
            {
                m_Serial.Write(sendMsg);

                lock (_SequenceLock)
                {
                    executeEvent = new AutoResetEvent(false);
                    m_SequenceQueue.Enqueue(executeEvent);
                }
            }

            var tmpArr = this.ReceiveMsg(executeEvent, data.WordCount);

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
            receiveData = new PLCReceivingPacket(readVal, data.DeviceCode, data.Address);
        }
        private void SendMsg(IEnumerable<PLCSendingPacket> dataList)
        {
            string sendMsg = string.Empty;

            string strHeader = string.Empty;
            string strData = string.Empty;

            var boolDataList = dataList.Where(item => item.Value is IEnumerable<bool> || item.Value is bool);
            var objDataList = dataList.Where(item => !(item.Value is IEnumerable<bool> || item.Value is bool));

            if (boolDataList.Count() > 0)
            {
                byte bitCount = 0;
                strHeader = "F8" + HostStationNo.ToString("X2") + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00" + "00";
                strData = "1402" + "0001";
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
                var checksumMsg = this.CalculateCheckSum(strHeader + strData);
                sendMsg = PREFIX_STRING + strHeader + strData + checksumMsg + POSTFIX_STRING;

                AutoResetEvent executeEvent = null;
                lock (_CommunicationLock)
                {
                    m_Serial.Write(sendMsg);
                    lock (_SequenceLock)
                    {
                        executeEvent = new AutoResetEvent(false);
                        m_SequenceQueue.Enqueue(executeEvent);
                    }
                }
                this.ReceiveMsg(executeEvent);
            }

            if (objDataList.Count() > 0)
            {
                byte wordCount = 0;
                byte dwordCount = 0;

                strHeader = "F8" + HostStationNo.ToString("X2") + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00" + "00";
                strData = "1402" + "0000";
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
                var checksumMsg = this.CalculateCheckSum(strHeader + strData);
                sendMsg = PREFIX_STRING + strHeader + strData + checksumMsg + POSTFIX_STRING;

                AutoResetEvent executeEvent = null;
                lock (_CommunicationLock)
                {
                    m_Serial.Write(sendMsg);
                    lock (_SequenceLock)
                    {
                        executeEvent = new AutoResetEvent(false);
                        m_SequenceQueue.Enqueue(executeEvent);
                    }
                }
                this.ReceiveMsg(executeEvent);
            }
        }
        private void SendMsg(IEnumerable<PLCSendingPacket> dataList, ref List<PLCReceivingPacket> readValArr)
        {
            string sendMsg = string.Empty;

            string strHeader = string.Empty;
            string strData = string.Empty;
            byte wordCount = 0;
            byte dwordCount = 0;

            strHeader = "F8" + HostStationNo.ToString("X2") + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00" + "00";
            strData = "0403" + "0000";
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
            var checksumMsg = this.CalculateCheckSum(strHeader + strData);
            sendMsg = PREFIX_STRING + strHeader + strData + checksumMsg + POSTFIX_STRING;


            AutoResetEvent executeEvent = null;
            lock (_CommunicationLock)
            {
                m_Serial.Write(sendMsg);
                lock (_SequenceLock)
                {
                    executeEvent = new AutoResetEvent(false);
                    m_SequenceQueue.Enqueue(executeEvent);
                }
            }

            var tmpArr = this.ReceiveMsg(executeEvent, (ushort)(dwordCount * 2 + wordCount));

            var wordArr = tmpArr.Take(wordCount * 2);
            var dwordArr = tmpArr.Skip(wordCount * 2);

            List<PLCReceivingPacket> tmpResultList = new List<PLCReceivingPacket>();

            foreach (var data in dataList)
            {
                List<byte> tmpList = new List<byte>();

                var tmpDwordCount = data.WordCount / 2;
                var tmpWordCount = data.WordCount % 2;

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
            }
            readValArr = tmpResultList;
        }
        private string CalculateCheckSum(string msg)
        {
            int sum = 0;
            foreach (var c in msg)
            {
                sum += c;
            }
            var res = sum.ToString("X2");

            return res.Substring(res.Length - 2, 2);
        }
        private byte[] ReceiveMsg(AutoResetEvent executeEvent, ushort wordCount = 0)
        {
            string receiveData = string.Empty;
            string tmpHeader = string.Empty;

            while (!executeEvent.WaitOne(100))
            {
                if (m_TerminateEvent.WaitOne(1)) throw new Exception("Session disconnected.");
            }
            lock (_CommunicationLock)
            {
                receiveData = m_CurrentString;
                m_CurrentString = string.Empty;
            }



            if (wordCount == 0 && receiveData.First().Equals((char)ACK))
            {
                tmpHeader = m_Serial.Encoding.GetString(new byte[] {ACK }) + "F8" + HostStationNo.ToString("X2") + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00" + "00";
                if (!receiveData.Contains(tmpHeader)) throw new Exception("Received wrong message. (Different header.)" + Environment.NewLine + "Message : " + receiveData);
                receiveData = "0000";
            }
            else if(receiveData.First().Equals((char)STX))
            {
                tmpHeader = m_Serial.Encoding.GetString(new byte[] { STX }) + "F8" + HostStationNo.ToString("X2") + NetworkNo.ToString("X2") + PCNo.ToString("X2") + "03FF" + "00" + "00";

                if (!receiveData.Contains(tmpHeader)) throw new Exception("Received wrong message. (Different header.)" + Environment.NewLine + "Message : " + receiveData);

                var tmpData = receiveData.Substring(1, receiveData.Length - (1 + 2 + POSTFIX_STRING.Length));
                var receivedChecksum = receiveData.Substring(receiveData.Length - (2 + POSTFIX_STRING.Length), 2);         
                if(!receivedChecksum.Equals(this.CalculateCheckSum(tmpData))) throw new Exception("Different checksum. (Maybe corrupted.)" + Environment.NewLine + "Message : " + receiveData);

                var tmpEtx = receiveData.IndexOf((char)ETX);
                if(tmpEtx == -1) throw new Exception("Received wrong message. (not include ETX in message.)" + Environment.NewLine + "Message : " + receiveData);
                receiveData = receiveData.Substring(tmpHeader.Length, receiveData.Length - tmpHeader.Length - (receiveData.Length - tmpEtx));
            }
            else if(receiveData.First().Equals((char)NAK)) throw new Exception("Received error message." + Environment.NewLine + "Error code : " + receiveData.Substring(receiveData.Length - (4 + POSTFIX_STRING.Length), 4));
            else throw new Exception("Invalid message format." + Environment.NewLine + "Message : " + receiveData);


            return PLCConverter.ConvertHexStringToByteArray(receiveData);
        }
        #endregion

        #endregion
    }
}
