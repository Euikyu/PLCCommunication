using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Mitsubishi.Controls
{
    /// <summary>
    /// Enum related to available data types.
    /// </summary>
    public enum EParseDataType
    {
#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        Byte,
        Boolean,
        Short,
        Int,
        Long,
        Float,
        Double,
        String
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
    }
    
    /// <summary>
    /// Command to sending messages to PLC at using tester.
    /// </summary>
    public class SendCommand
    {
        #region Fields
        private int m_DeviceNumber;
        private string m_DeviceHexNumber;
        #endregion

        #region Properties
        /// <summary>
        /// Defining the PLC device code to send.
        /// </summary>
        public EPLCDeviceCode DeviceCode { get; set; }
        /// <summary>
        /// Address of device to send.
        /// </summary>
        public int DeviceNumber
        {
            get
            {
                if ((int)DeviceCode >= (int)EPLCDeviceCode.X && (int)DeviceCode <= (int)EPLCDeviceCode.DY)
                {
                    if (m_DeviceHexNumber != null && int.TryParse(m_DeviceHexNumber, System.Globalization.NumberStyles.HexNumber, null, out int num))
                    {
                        return num;
                    }
                    else return 0;
                }
                else return m_DeviceNumber;
            }
            set
            {
                m_DeviceHexNumber = value.ToString("X");
                m_DeviceNumber = value;
            }
        }
        /// <summary>
        /// Hex decimal address of device to send.
        /// (it use external contact address.)
        /// </summary>
        public string DeviceHexNumber
        {
            get
            {
                if ((int)DeviceCode >= (int)EPLCDeviceCode.X && (int)DeviceCode <= (int)EPLCDeviceCode.DY) return m_DeviceHexNumber;
                else return m_DeviceNumber.ToString("X");
            }
            set
            {
                m_DeviceHexNumber = value;
                if (int.TryParse(m_DeviceHexNumber, System.Globalization.NumberStyles.HexNumber, null, out int num)) m_DeviceNumber = num;
                else m_DeviceNumber = 0;
            }
        }
        /// <summary>
        /// Word count to read. 
        /// (Only use to reading data.)
        /// </summary>
        public ushort WordCount { get; set; }
        /// <summary>
        /// Data type of values to parse.
        /// </summary>
        public EParseDataType DataType { get; set; }
        /// <summary>
        /// Parsed data text.
        /// </summary>
        public string Value { get; set; }
        #endregion

        /// <summary>
        /// Generate send command instance at using tester.
        /// </summary>
        public SendCommand()
        {
            DeviceCode = EPLCDeviceCode.M;
            DataType = EParseDataType.Short;
            Value = string.Empty;
        }
    }

    /// <summary>
    /// Data containing messages received from PLC at using tester.
    /// </summary>
    public class ResultData : INotifyPropertyChanged
    {
        #region Fields
        private PLCReceivingPacket m_ReceiveData;
        private EParseDataType m_DataType;
        private char m_SeparateChar;

#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        #endregion

        #region Properties
        /// <summary>
        /// Defining the PLC device code read.
        /// </summary>
        public EPLCDeviceCode DeviceCode
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.DeviceCode;
                else return EPLCDeviceCode.M;
            }
        }
        /// <summary>
        /// Address of device read.
        /// </summary>
        public int DeviceNumber
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.Address;
                else return -1;
            }
        }
        /// <summary>
        /// Hex decimal address of device read.
        /// </summary>
        public string DeviceHexNumber
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.Address.ToString("X");
                else return "-1";
            }
        }
        /// <summary>
        /// Separation character between values.
        /// </summary>
        public char SeparateChar
        {
            get { return m_SeparateChar; }
            set
            {
                if (m_SeparateChar != value)
                {
                    m_SeparateChar = value;
                    if (m_ReceiveData != null) this.ChangeResultText();
                }
            }
        }
        /// <summary>
        /// Data type of values to parse.
        /// </summary>
        public EParseDataType DataType
        {
            get
            {
                return m_DataType;
            }
            set
            {
                m_DataType = value;
                if (m_ReceiveData != null)
                {
                    this.ChangeResultText();
                }
            }
        }
        /// <summary>
        /// Parsed data text.
        /// </summary>
        public string ResultText { get; private set; }
        #endregion

        /// <summary>
        /// Generate Data containing messages received from PLC.
        /// </summary>
        /// <param name="receiveData">Received data.</param>
        /// <param name="separateChar">Character to separate between values.</param>
        public ResultData(PLCReceivingPacket receiveData, char separateChar)
        {
            this.m_ReceiveData = receiveData;
            this.SeparateChar = separateChar;
            DataType = EParseDataType.Byte;
        }

        #region Methods
        private void ChangeResultText()
        {
            ResultText = string.Empty;

            switch (m_DataType)
            {
                case EParseDataType.Boolean:
                    var boolArr = m_ReceiveData.GetBooleanArray();
                    foreach (var item in boolArr) ResultText += (item ? "True" : "False") + m_SeparateChar;
                    break;

                case EParseDataType.Short:
                    var shortArr = m_ReceiveData.GetInt16Array();
                    foreach (var item in shortArr) ResultText += item.ToString() + m_SeparateChar;
                    break;

                case EParseDataType.Int:
                    var intArr = m_ReceiveData.GetInt32Array();
                    foreach (var item in intArr) ResultText += item.ToString() + m_SeparateChar;
                    break;

                case EParseDataType.Float:
                    var floatArr = m_ReceiveData.GetSingleArray();
                    foreach (var item in floatArr) ResultText += item.ToString() + m_SeparateChar;
                    break;

                case EParseDataType.Long:
                    var longArr = m_ReceiveData.GetInt64Array();
                    foreach (var item in longArr) ResultText += item.ToString() + m_SeparateChar;
                    break;

                case EParseDataType.Double:
                    var doubleArr = m_ReceiveData.GetDoubleArray();
                    foreach (var item in doubleArr) ResultText += item.ToString() + m_SeparateChar;
                    break;

                case EParseDataType.String:
                    ResultText = m_ReceiveData.GetASCIIString() + m_SeparateChar;
                    break;

                case EParseDataType.Byte:
                default:
                    var byteArr = m_ReceiveData.GetByteArray();
                    foreach (var item in byteArr) ResultText += item.ToString("X2") + m_SeparateChar;
                    break;
            }

            if (ResultText.Length > 0) ResultText = ResultText.Remove(ResultText.Length - 1, 1);

            this.RaisePropertyChanged(nameof(ResultText));
        }

        #endregion
    }
}
