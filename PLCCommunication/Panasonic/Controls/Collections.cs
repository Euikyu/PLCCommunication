using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Panasonic.Controls
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
    /// Enum related to PLC device code with writable contact.
    /// </summary>
    public enum EContactWritableDeviceCode
    {
        /// <summary>
        /// External output contact. (only use ASCII)
        /// </summary>
        Y = EPLCDeviceCode.Y,
        /// <summary>
        /// Internal relay contact. (only use ASCII)
        /// </summary>
        R = EPLCDeviceCode.R,
        /// <summary>
        /// Link relay contact. (only use ASCII)
        /// </summary>
        L = EPLCDeviceCode.C_L
    }
    /// <summary>
    /// Enum related to PLC device code with readable contact.
    /// </summary>
    public enum EContactReadableDeviceCode
    {
        /// <summary>
        /// Timer value. (only use ASCII and read-only)
        /// </summary>
        T = EPLCDeviceCode.T,
        /// <summary>
        /// Counter value. (only use ASCII and read-only)
        /// </summary>
        C = EPLCDeviceCode.C,
        /// <summary>
        /// External input contact. (only use ASCII and read-only)
        /// </summary>
        X = EPLCDeviceCode.X,
        /// <summary>
        /// External output contact. (only use ASCII)
        /// </summary>
        Y = EPLCDeviceCode.Y,
        /// <summary>
        /// Internal relay contact. (only use ASCII)
        /// </summary>
        R = EPLCDeviceCode.R,
        /// <summary>
        /// Link relay contact. (only use ASCII)
        /// </summary>
        L = EPLCDeviceCode.C_L
    }
    /// <summary>
    /// Enum related to PLC device code with data area.
    /// </summary>
    public enum EDataDeviceCode
    {
        /// <summary>
        /// Link register. (only use ASCII)
        /// </summary>
        L = EPLCDeviceCode.L,
        /// <summary>
        /// Data register. (only use ASCII)
        /// </summary>
        D = EPLCDeviceCode.D,
        /// <summary>
        /// File register. (only use ASCII)
        /// </summary>
        F = EPLCDeviceCode.F
    }
    /// <summary>
    /// Enum related to PLC device code with index register.
    /// </summary>
    public enum EIndexRegisterDeviceCode
    {
        /// <summary>
        /// Index 0 register. (only use ASCII)
        /// </summary>
        IX = EPLCDeviceCode.IX,
        /// <summary>
        /// Index 1 register. (only use ASCII)
        /// </summary>
        IY = EPLCDeviceCode.IY,
        /// <summary>
        /// Index 0, 1 both register. (only use ASCII)
        /// </summary>
        ID = EPLCDeviceCode.ID
    }
    /// <summary>
    /// Enum related to PLC device code with binary communication.
    /// </summary>
    public enum EBinaryDeviceCode
    {
        /// <summary>
        /// Link relay contact. (only use binary)
        /// </summary>
        WL = EPLCDeviceCode.WL,
        /// <summary>
        /// Internal relay contact. (only use binary)
        /// </summary>
        WR = EPLCDeviceCode.WR,
        /// <summary>
        /// External output relay contact. (only use binary)
        /// </summary>
        WY = EPLCDeviceCode.WY,
        /// <summary>
        /// External input relay contact. (only use binary)
        /// </summary>
        WX = EPLCDeviceCode.WX,
        /// <summary>
        /// Timer/Counter setting value. (only use binary)
        /// </summary>
        SV = EPLCDeviceCode.SV,
        /// <summary>
        /// Timer/Counter Progress value. (only use binary)
        /// </summary>
        EV = EPLCDeviceCode.EV,
        /// <summary>
        /// Link register. (only use binary)
        /// </summary>
        LD = EPLCDeviceCode.LD,
        /// <summary>
        /// Special relay contact. (only use binary)
        /// </summary>
        S_WR = EPLCDeviceCode.S_WR,
        /// <summary>
        /// Special data register. (only use binary)
        /// </summary>
        S_DT = EPLCDeviceCode.S_DT,
        /// <summary>
        /// Data register. (only use binary)
        /// </summary>
        DT = EPLCDeviceCode.DT,
        /// <summary>
        /// File register. (only use binary)
        /// </summary>
        FL = EPLCDeviceCode.FL,
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
        public int Address { get; set; }
        /// <summary>
        /// Contact address of device to send.
        /// </summary>
        public string ContactAddress { get; set; }
        /// <summary>
        /// Value to determine that is contact.
        /// </summary>
        public bool IsContact { get; set; }
        public ushort WordCount { get; set; }
        public EParseDataType DataType { get; set; }
        public string Value { get; set; }
        #endregion

        public SendCommand()
        {
            DeviceCode = EPLCDeviceCode.D;
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
        public EPLCDeviceCode DeviceCode
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.DeviceCode;
                else return EPLCDeviceCode.D;
            }
        }
        public int DeviceNumber
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.Address;
                else return 0;
            }
        }
        public string DeviceHexNumber
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.ContactAddress;
                else return "0000";
            }
        }

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
        public string ResultText { get; private set; }
        #endregion

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
                    ResultText = m_ReceiveData.GetASCIIString();
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

