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

namespace PLCCommunication.Panasonic
{
    public class SerialPLC : IPLC
    {
        #region Fields

        private readonly string CR = Encoding.ASCII.GetString(new byte[] { 0x0D });
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

        public string PortName
        {
            get { return m_Setting == null ? string.Empty : m_Setting.PortName; }
            set { if (m_Setting != null) m_Setting.PortName = value; }
        }
        public int BaudRate
        {
            get { return m_Setting == null ? -1 : m_Setting.BaudRate; }
            set { if (m_Setting != null) m_Setting.BaudRate = value; }
        }
        public int DataBits
        {
            get { return m_Setting == null ? -1 : m_Setting.DataBits; }
            set { if (m_Setting != null) m_Setting.DataBits = value; }
        }
        public Parity Parity
        {
            get { return m_Setting == null ? Parity.None : m_Setting.Parity; }
            set { if (m_Setting != null) m_Setting.Parity = value; }
        }
        public StopBits StopBits
        {
            get { return m_Setting == null ? StopBits.One : m_Setting.StopBits; }
            set { if (m_Setting != null) m_Setting.StopBits = value; }
        }
        public Handshake Handshake
        {
            get { return m_Setting == null ? Handshake.None : m_Setting.Handshake; }
            set { if (m_Setting != null) m_Setting.Handshake = value; }
        }
        public byte UnitNo
        {
            get { return m_Setting == null ? byte.MinValue : m_Setting.UnitNo; }
            set { if (m_Setting != null) m_Setting.UnitNo = value; }
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

        public SerialPLC()
        {
            m_Setting = new SerialSetting();

            this.PortName = "COM1";
            this.BaudRate = 9600;
            this.DataBits = 8;
            this.Parity = Parity.None;
            this.StopBits = StopBits.One;
            this.Handshake = Handshake.None;
        }
        public SerialPLC(string portName, int baudRate, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, Handshake handshake = Handshake.None)
        {
            m_Setting = new SerialSetting();

            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;
            this.Handshake = handshake;
        }


#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        public void Dispose()
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        {
            this.Disconnect();
            if (m_Serial != null) m_Serial.Dispose();
        }


        #region Methods
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
        public void Save()
        {
            Directory.CreateDirectory(_DefaultPath);
            XmlSerializer serializer = new XmlSerializer(typeof(SerialSetting));
            using (var sw = new StreamWriter(_DefaultPath + @"\Mitsubishi_Serial.xml"))
            {
                serializer.Serialize(sw, m_Setting ?? new SerialSetting());
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

            XmlSerializer serializer = new XmlSerializer(typeof(SerialSetting));
            using (var sw = new StreamWriter(filePath))
            {
                serializer.Serialize(sw, m_Setting ?? new SerialSetting());
            }
        }

        public void Connect()
        {
            if (IsConnected != null) throw new Exception("Already Opened.");

            this.SerialConnect();

            if (m_ConnectionCheckThread == null || !m_ConnectionCheckThread.IsAlive)
            {
                m_ConnectionCheckThread = new Thread(new ThreadStart(this.OnCheckProcess));
                m_ConnectionCheckThread.Start();
            }
        }
        public void Connect(string portName, int baudRate)
        {
            if (IsConnected != null) throw new Exception("Already Opened.");

            this.PortName = portName;
            this.BaudRate = baudRate;

            this.Connect();
        }
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
            while ((len = m_StringBuffer.IndexOf(CR)) != -1)
            {
                len = len + CR.Length > m_StringBuffer.Length ? m_StringBuffer.Length : len + CR.Length;

                lock (_CommunicationLock)
                {
                    m_CurrentString = m_StringBuffer.Substring(0, len);
                    m_StringBuffer = m_StringBuffer.Remove(0, len);
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

        public void Disconnect()
        {
            if (m_ConnectionCheckThread != null && m_ConnectionCheckThread.IsAlive)
            {
                m_ConnectionCheckThread.Abort();
                m_ConnectionCheckThread.Join(1000);
            }
            this.SerialDisconnect();
        }
        private void ClearMessages()
        {
            lock (_SequenceLock) if (m_SequenceQueue != null) m_SequenceQueue.Clear();
            if (m_Serial != null && m_Serial.IsOpen)
            {
                m_Serial.DiscardInBuffer();
                m_Serial.DiscardOutBuffer();
            }
            m_TerminateEvent.Set();
        }
        private void SerialDisconnect()
        {
            this.ClearMessages();
            if (m_Serial != null)
            {
                m_Serial.DataReceived -= this.Serial_DataReceived;
                m_Serial.Close();
            }
        }
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


        #region Communicate Methods
        private void SendMsg(PLCSendingPacket data)
        {
            string sendMsg = string.Empty;
            Type dataType = data.Value.GetType();

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
            sendMsg = strHeader + strData + PLCConverter.EncodeBCC(strHeader + strData) + CR;

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

            string strHeader = "%" + UnitNo.ToString("00") + "#R";
            string strData = string.Empty;

            if ((int)data.DeviceCode < (int)EPLCDeviceCode.T) throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            else if ((int)data.DeviceCode <= (int)EPLCDeviceCode.C_L)
            {
                strHeader += "C";
                strData += "C" + PLCConverter.ConvertStringFromAddress(data, data.WordCount);
            }
            else if ((int)data.DeviceCode <= (int)EPLCDeviceCode.F)
            {
                strHeader += "D";
                strData += PLCConverter.ConvertStringFromAddress(data, data.WordCount);
            }
            else
            {
                strHeader += "D";
                strData += PLCConverter.ConvertStringFromAddress(data);
            }

            sendMsg = strHeader + strData + PLCConverter.EncodeBCC(strHeader + strData) + CR;
            
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
            receiveData = new PLCReceivingPacket(readVal, data.DeviceCode, data.ContactAddress);
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

            if (receiveData.Contains("!")) throw new Exception("Received error message." + Environment.NewLine + "Message : " + receiveData.Substring(4, 2));
            if (!PLCConverter.DecodeBCC(receiveData.Substring(0, receiveData.Length - 3), receiveData.Substring(receiveData.Length - 3, 2))) throw new Exception("Received wrong message. (Different BCC.)" + Environment.NewLine + "Message : " + receiveData);

            if (receiveData.Contains("%FF") || receiveData.Contains("<FF")) return new byte[0];
            if (receiveData.Contains("$R"))
            {
                if (wordCount != receiveData.Substring(6, receiveData.Length - 3).Length / 4) throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + receiveData);
                return PLCConverter.ConvertHexStringToByteArray(receiveData.Substring(6, receiveData.Length - 3));
            }
            else if (receiveData.Contains("$W"))
            {
                return new byte[0];
            }
            else
            {
                throw new Exception("Received wrong message. (Unsupported format.)" + Environment.NewLine + "Message : " + receiveData);
            }
        }
        #endregion

        #endregion
    }
}
