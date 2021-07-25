using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Panasonic.Controls
{
    public enum EParseDataType
    {
        Byte,
        Boolean,
        Short,
        Int,
        Long,
        Float,
        Double,
        String
    }
    public class SendCommand
    {
        private int m_DeviceNumber;
        private string m_DeviceHexNumber;

        public EPLCDeviceCode DeviceCode { get; set; }
        public int Address { get; set; }
        public string ContactAddress { get; set; }
        public ushort WordCount { get; set; }
        public EParseDataType DataType { get; set; }
        public string Value { get; set; }

        public SendCommand()
        {
            DeviceCode = EPLCDeviceCode.D;
            DataType = EParseDataType.Short;
            Value = string.Empty;
        }
    }

    public class ResultData : INotifyPropertyChanged
    {
        #region Fields
        private PLCReceivingPacket m_ReceiveData;
        private EParseDataType m_DataType;
        private char m_SeparateChar;

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Properties
        public EPLCDeviceCode DeviceCode
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.DeviceCode;
                else return EPLCDeviceCode.M;
            }
        }
        public int DeviceNumber
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.Address;
                else return -1;
            }
        }
        public string DeviceHexNumber
        {
            get
            {
                if (m_ReceiveData != null) return m_ReceiveData.Address.ToString("X");
                else return "-1";
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

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

