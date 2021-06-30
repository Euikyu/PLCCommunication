using System;
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
    public class SerialPLC : IPLC
    {
        #region Fields
        private readonly string _DefaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Check Box";

        private SerialPort m_Serial;
        private SerialSetting m_Setting;
        private Thread m_ConnectionCheckThread;


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
        public bool IsConnected
        {
            get
            {
                if (m_Serial != null) return m_Serial.IsOpen;
                else return false;
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
            this.Timeout = 1000;
        }
        public SerialPLC(string portName, int baudRate, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, Handshake handshake = Handshake.None, uint timeout = 4000)
        {
            m_Setting = new SerialSetting();

            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;
            this.Handshake = handshake;
            this.Timeout = timeout;
        }

        public void Dispose()
        {
            this.Disconnect();
            if(m_Serial != null) m_Serial.Dispose();
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
            if (IsConnected) throw new Exception("Already connected.");

            this.SerialConnect();

            if(m_ConnectionCheckThread == null || !m_ConnectionCheckThread.IsAlive)
            {
                m_ConnectionCheckThread = new Thread(new ThreadStart(this.OnCheckProcess));
                m_ConnectionCheckThread.Start();
            }
        }
        public void Connect(string portName, int baudRate)
        {
            if (IsConnected) throw new Exception("Already connected.");

            this.PortName = portName;
            this.BaudRate = baudRate;

            this.Connect();
        }
        public void Connect(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, Handshake handshake)
        {
            if (IsConnected) throw new Exception("Already connected.");

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
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if(e.EventType == SerialData.Chars)
            {
                var aa = m_Serial.Encoding.GetBytes(m_Serial.ReadExisting());
                
            }
            else
            {
                this.Disconnect();
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
                    if (!(bool)IsConnected)
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

        public void Disconnect()
        {

        }
        private void SerialDisconnect()
        {
            if(m_Serial != null)
            {
                m_Serial.DataReceived -= this.Serial_DataReceived;
                m_Serial.DiscardInBuffer();
                m_Serial.DiscardOutBuffer();
                m_Serial.Close();
            }
        }
        public void Refresh()
        {

        }

        #endregion
    }
}
