using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PLCCommunication.Mitsubishi.Controls
{
    /// <summary>
    /// SocketTester.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SocketTester : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private SocketPLC m_PLC;

        private Thread m_ConnectionCheckThread;

        private char m_SeparateChar;
        private int m_SelectedSeparateCharIndex;
        private bool m_TestBool;
        #endregion

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string IP
        {
            get
            {
                if (m_PLC != null) return m_PLC.IP;
                else return string.Empty;
            }
            set
            {
                if (m_PLC != null)
                {
                    m_PLC.IP = value;
                }
            }
        }
        public ushort Port
        {
            get
            {
                if (m_PLC != null) return (ushort)m_PLC.PortNumber;
                else return 0;
            }
            set
            {
                if (m_PLC != null)
                {
                    m_PLC.PortNumber = value;
                }
            }
        }
        public bool IsBinary
        {
            get
            {
                if (m_PLC != null) return m_PLC.ProtocolFormat == EPLCProtocolFormat.Binary;
                else return true;
            }
            set
            {
                if (m_PLC != null)
                {
                    if (value)
                    {
                        m_PLC.ProtocolFormat = EPLCProtocolFormat.Binary;
                    }
                    else
                    {
                        m_PLC.ProtocolFormat = EPLCProtocolFormat.ASCII;

                    }
                }
            }
        }
        public byte NetworkNo
        {
            get
            {
                if (m_PLC != null) return m_PLC.NetworkNo;
                else return 0;
            }
            set
            {
                if (m_PLC != null)
                {
                    m_PLC.NetworkNo = value;
                }
            }
        }
        public byte PCNo
        {
            get
            {
                if (m_PLC != null) return m_PLC.PCNo;
                else return 255;
            }
            set
            {
                if (m_PLC != null)
                {
                    m_PLC.PCNo = value;
                }
            }
        }
        public bool IsConnected
        {
            get
            {
                if (m_PLC != null) return m_PLC.IsConnected;
                else return false;
            }
        }
        public int SelectedSeparateCharIndex
        {
            get
            {
                return m_SelectedSeparateCharIndex;
            }
            set
            {
                if (m_SelectedSeparateCharIndex != value)
                {
                    m_SelectedSeparateCharIndex = value;
                    switch (value)
                    {
                        case 0:
                            m_SeparateChar = ' ';
                            break;
                        case 1:
                            m_SeparateChar = '-';
                            break;
                        case 2:
                            m_SeparateChar = ',';
                            break;
                        case 3:
                            m_SeparateChar = '_';
                            break;
                    }
                    foreach (var res in ResultList)
                    {
                        res.SeparateChar = m_SeparateChar;
                    }
                }
            }
        }
        
        //Collections
        public ObservableCollection<SendCommand> WriteCommandList { get; set; }
        public ObservableCollection<SendCommand> ReadCommandList { get; set; }
        public ObservableCollection<ResultData> ResultList { get; set; }
        #endregion

        public SocketTester()
        {
            InitializeComponent();
            DataContext = this;

            m_PLC = new SocketPLC();
            WriteCommandList = new ObservableCollection<SendCommand>();
            ReadCommandList = new ObservableCollection<SendCommand>();
            ResultList = new ObservableCollection<ResultData>();
            m_SeparateChar = ' ';
        }

        #region Methods
        
        #region Events
        private void Tester_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                this.OnClose();
            }
            else
            {
                this.OnLoad();
            }
        }

        private void Connect_button_Click(object sender, RoutedEventArgs e)
        {
            this.Connect_button_Click();
        }

        private void Write_button_Click(object sender, RoutedEventArgs e)
        {
            this.Write_button_Click();
        }

        private void Read_button_Click(object sender, RoutedEventArgs e)
        {
            this.Read_button_Click();
        }

        private void NewWriteCommand_button_Click(object sender, RoutedEventArgs e)
        {
            this.NewWriteCommand_button_Click();
        }

        private void DeleteWriteCommand_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteWriteCommand_button_Click(write_dataGrid.SelectedIndex);
        }

        private void NewReadCommand_button_Click(object sender, RoutedEventArgs e)
        {
            this.NewReadCommand_button_Click();
        }

        private void DeleteReadCommand_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteReadCommand_button_Click(read_dataGrid.SelectedIndex);
        }
        #endregion
        private void UpdateProperties()
        {
            this.RaisePropertyChanged(nameof(this.IP));
            this.RaisePropertyChanged(nameof(this.Port));
            this.RaisePropertyChanged(nameof(this.IsBinary));
            this.RaisePropertyChanged(nameof(this.NetworkNo));
            this.RaisePropertyChanged(nameof(this.PCNo));
            this.RaisePropertyChanged(nameof(this.SelectedSeparateCharIndex));
        }
        internal void OnLoad()
        {
            m_PLC.Load();
            this.UpdateProperties();
            if (m_ConnectionCheckThread == null || !m_ConnectionCheckThread.IsAlive)
            {
                m_ConnectionCheckThread = new Thread(new ThreadStart(() =>
                {
                    while (true)
                    {
                        this.RaisePropertyChanged(nameof(IsConnected));
                        Thread.Sleep(100);
                    }
                }))
                {
                    Name = "UI_ConnectionCheck"
                };
                m_ConnectionCheckThread.Start();
            }
        }

        internal void OnClose()
        {
            if (m_ConnectionCheckThread != null && m_ConnectionCheckThread.IsAlive)
            {
                m_ConnectionCheckThread.Abort();
                m_ConnectionCheckThread.Join(1000);
            }
            if (m_PLC != null)
            {
                m_PLC.Save();
                m_PLC.Dispose();
            }
        }

        internal void Connect_button_Click()
        {
            if (IsConnected)
            {
                try
                {
                    m_PLC.Disconnect();
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
            else
            {
                try
                {
                    m_PLC.Connect(IP, Port);
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }

        internal void Write_button_Click()
        {
            try
            {
                if (WriteCommandList.Count == 1)
                {
                    var data = this.ConvertPLCData(WriteCommandList, 0);
                    m_PLC.Write(data);

                }
                else if (WriteCommandList.Count > 0)
                {
                    List<PLCSendingPacket> dataList = new List<PLCSendingPacket>();
                    for (int i = 0; i < WriteCommandList.Count; i++)
                    {
                        dataList.Add(this.ConvertPLCData(WriteCommandList, i));
                    }
                    m_PLC.Write(dataList);
                }

                MessageBox.Show("Write Successfully.");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        internal void Read_button_Click()
        {
            try
            {
                if (ReadCommandList.Count == 1)
                {
                    ResultList.Clear();
                    var data = this.ConvertPLCData(ReadCommandList, 0, true);
                    PLCReceivingPacket res = null;
                    m_PLC.Read(data, ref res);
                    ResultList.Add(new ResultData(res, m_SeparateChar));
                }
                else if (ReadCommandList.Count > 0)
                {
                    ResultList.Clear();
                    List<PLCSendingPacket> dataList = new List<PLCSendingPacket>();
                    List<PLCReceivingPacket> resList = null;
                    for (int i = 0; i < ReadCommandList.Count; i++)
                    {
                        dataList.Add(this.ConvertPLCData(ReadCommandList, i, true));
                    }
                    m_PLC.Read(dataList, ref resList);
                    for (int i = 0; i < resList.Count; i++)
                    {
                        ResultList.Add(new ResultData(resList[i], m_SeparateChar));
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        private PLCSendingPacket ConvertPLCData(ObservableCollection<SendCommand> sendList, int index, bool isRead = false)
        {
            if (!isRead)
            {
                object val = null;
                string[] tmpBuf = sendList[index].Value.Split(m_SeparateChar);
                switch (sendList[index].DataType)
                {
                    case EParseDataType.Boolean:
                        List<bool> boolList = new List<bool>();
                        foreach (var str in tmpBuf)
                        {
                            if (bool.TryParse(str, out bool res)) boolList.Add(res);
                        }
                        val = boolList;
                        break;
                    case EParseDataType.Byte:
                        List<byte> byteList = new List<byte>();
                        foreach (var str in tmpBuf)
                        {
                            if (byte.TryParse(str, out byte res)) byteList.Add(res);
                        }
                        val = byteList;
                        break;
                    case EParseDataType.Short:
                        List<short> shortList = new List<short>();
                        foreach (var str in tmpBuf)
                        {
                            if (short.TryParse(str, out short res)) shortList.Add(res);
                        }
                        val = shortList;
                        break;
                    case EParseDataType.Int:
                        List<int> intList = new List<int>();
                        foreach (var str in tmpBuf)
                        {
                            if (int.TryParse(str, out int res)) intList.Add(res);
                        }
                        val = intList;
                        break;
                    case EParseDataType.Long:
                        List<long> longList = new List<long>();
                        foreach (var str in tmpBuf)
                        {
                            if (long.TryParse(str, out long res)) longList.Add(res);
                        }
                        val = longList;
                        break;
                    case EParseDataType.Float:
                        List<float> floatList = new List<float>();
                        foreach (var str in tmpBuf)
                        {
                            if (float.TryParse(str, out float res)) floatList.Add(res);
                        }
                        val = floatList;
                        break;
                    case EParseDataType.Double:
                        List<double> doubleList = new List<double>();
                        foreach (var str in tmpBuf)
                        {
                            if (double.TryParse(str, out double res)) doubleList.Add(res);
                        }
                        val = doubleList;
                        break;
                    case EParseDataType.String:
                        val = sendList[index].Value;
                        break;
                }
                return new PLCSendingPacket(sendList[index].DeviceCode, sendList[index].DeviceNumber, val);
            }
            else
            {
                return new PLCSendingPacket(sendList[index].DeviceCode, sendList[index].DeviceNumber, isRead, sendList[index].WordCount);
            }
        }

        internal void NewWriteCommand_button_Click()
        {
            WriteCommandList.Add(new SendCommand());
        }
        internal void DeleteWriteCommand_button_Click(int selectedIndex)
        {
            if (selectedIndex != -1 && WriteCommandList.Count > selectedIndex)
            {
                WriteCommandList.RemoveAt(selectedIndex);
            }
        }

        internal void NewReadCommand_button_Click()
        {
            ReadCommandList.Add(new SendCommand());
        }
        internal void DeleteReadCommand_button_Click(int selectedIndex)
        {
            if (selectedIndex != -1 && ReadCommandList.Count > selectedIndex)
            {
                ReadCommandList.RemoveAt(selectedIndex);
            }
        }



        #endregion
    }
}
