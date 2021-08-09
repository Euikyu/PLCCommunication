using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Mitsubishi
{
    /// <summary>
    /// Enum related to PLC device code.
    /// </summary>
    public enum EPLCDeviceCode
    {
#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        M = 0x90,
        SM = 0x91,
        L = 0x92,
        F = 0x93,
        V = 0x94,
        X = 0x9C,
        Y = 0x9D,
        B = 0xA0,
        SB = 0xA1,
        DX = 0xA2,
        DY = 0xA3,
        D = 0xA8,
        SD = 0xA9,
        R = 0xAF,
        ZR = 0xB0,
        W = 0xB4,
        SW = 0xB5,
        TC = 0xC0,
        TS = 0xC1,
        TN = 0xC2,
        CC = 0xC3,
        CS = 0xC4,
        CN = 0xC5,
        Z = 0xCC,
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
    }

    /// <summary>
    /// Class related to PLC data to send.
    /// </summary>
    public class PLCSendingPacket : IPLCSendingPacket
    {
        #region Fields

        #endregion

        #region Properties
        /// <summary>
        /// Defining the PLC device code to use.
        /// </summary>
        public EPLCDeviceCode DeviceCode { get; }
        /// <summary>
        /// First address to use.
        /// </summary>
        public int Address { get; }
        /// <summary>
        /// Value to write.
        /// </summary>
        public object Value { get; }
        /// <summary>
        /// Data to determine to read or write.
        /// </summary>
        public bool IsRead { get; }
        /// <summary>
        /// Amount words to read.
        /// </summary>
        public ushort WordCount { get; }
        #endregion

        /// <summary>
        /// Generate PLC data with word units to read.
        /// </summary>
        /// <param name="code">PLC device code to use.</param>
        /// <param name="address">Address to use.</param>
        /// <param name="isRead">Proof to read.</param>
        /// <param name="readWordCount">Amount words to read.</param>
        public PLCSendingPacket(EPLCDeviceCode code, int address, bool isRead, ushort readWordCount)
        {
            this.DeviceCode = code;
            this.Address = address;
            this.Value = byte.MinValue;
            this.WordCount = readWordCount;
            this.IsRead = true;
        }

        /// <summary>
        /// Generate PLC data to write from this value &amp; code &amp; address.
        /// </summary>
        /// <param name="code">PLC device code to use.</param>
        /// <param name="address">Address to use.</param>
        /// <param name="value">Value to write.</param>
        public PLCSendingPacket(EPLCDeviceCode code, int address, object value)
        {
            this.DeviceCode = code;
            this.Address = address;
            this.Value = value;
            this.WordCount = 0;
            this.IsRead = false;
        }
    }


    /// <summary>
    /// Class related to PLC data to receive.
    /// </summary>
    public class PLCReceivingPacket : IPLCReceivingPacket
    {
        #region Fields
        private byte[] m_OriginValueArray;
        #endregion

        #region Properties
        /// <summary>
        /// Defining the PLC device code read.
        /// </summary>
        public EPLCDeviceCode DeviceCode { get; }
        /// <summary>
        /// First address read.
        /// </summary>
        public int Address { get; }
        #endregion

        /// <summary>
        /// Generate PLC data received from this address &amp; code.
        /// </summary>
        /// <param name="receivedData">Received data from PLC.</param>
        /// <param name="code">Data's PLC device code.</param>
        /// <param name="address">data's first address.</param>
        public PLCReceivingPacket(byte[] receivedData, EPLCDeviceCode code, int address)
        {
            this.m_OriginValueArray = receivedData.ToArray();
            this.DeviceCode = code;
            this.Address = address;
        }

        #region Methods
        /// <summary>
        /// Get byte array from origin values.
        /// </summary>
        /// <returns></returns>
        public byte[] GetByteArray()
        {
            if (m_OriginValueArray == null) return null;
            else return m_OriginValueArray.ToArray();
        }
        /// <summary>
        /// Get boolean array from origin values.
        /// </summary>
        /// <returns></returns>
        public bool[] GetBooleanArray()
        {
            if (m_OriginValueArray == null) return null;

            List<bool> tmpResult = new List<bool>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 2)
            {
                if (i + 1 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[2];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }

                    var tmpStr = Convert.ToString(BitConverter.ToInt16(remainsVal, 0), 2).PadLeft(16, '0').Reverse();
                    foreach (var tmpChar in tmpStr)
                    {
                        tmpResult.Add(tmpChar.Equals('1') ? true : false);
                    }
                }
                else
                {
                    var tmpStr = Convert.ToString(BitConverter.ToInt16(m_OriginValueArray, i), 2).PadLeft(16, '0').Reverse();
                    foreach (var tmpChar in tmpStr)
                    {
                        tmpResult.Add(tmpChar.Equals('1') ? true : false);
                    }
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get 16bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        public short[] GetInt16Array()
        {
            if (m_OriginValueArray == null) return null;

            List<short> tmpResult = new List<short>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 2)
            {
                if (i + 1 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[2];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }
                    tmpResult.Add(BitConverter.ToInt16(remainsVal, 0));
                }
                else
                {
                    tmpResult.Add(BitConverter.ToInt16(m_OriginValueArray, i));
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get unsigned 16bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        public ushort[] GetUInt16Array()
        {
            if (m_OriginValueArray == null) return null;

            List<ushort> tmpResult = new List<ushort>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 2)
            {
                if (i + 1 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[2];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }
                    tmpResult.Add(BitConverter.ToUInt16(remainsVal, 0));
                }
                else
                {
                    tmpResult.Add(BitConverter.ToUInt16(m_OriginValueArray, i));
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get 32bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        public int[] GetInt32Array()
        {
            if (m_OriginValueArray == null) return null;

            List<int> tmpResult = new List<int>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 4)
            {
                if (i + 3 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[4];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }
                    tmpResult.Add(BitConverter.ToInt32(remainsVal, 0));
                }
                else
                {
                    tmpResult.Add(BitConverter.ToInt32(m_OriginValueArray, i));
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get unsigned 32bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        public uint[] GetUInt32Array()
        {
            if (m_OriginValueArray == null) return null;

            List<uint> tmpResult = new List<uint>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 4)
            {
                if (i + 3 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[4];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }
                    tmpResult.Add(BitConverter.ToUInt32(remainsVal, 0));
                }
                else
                {
                    tmpResult.Add(BitConverter.ToUInt32(m_OriginValueArray, i));
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get 64bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        public long[] GetInt64Array()
        {
            if (m_OriginValueArray == null) return null;

            List<long> tmpResult = new List<long>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 8)
            {
                if (i + 7 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[8];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }
                    tmpResult.Add(BitConverter.ToInt64(remainsVal, 0));
                }
                else
                {
                    tmpResult.Add(BitConverter.ToInt64(m_OriginValueArray, i));
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get unsigned 64bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        public ulong[] GetUInt64Array()
        {
            if (m_OriginValueArray == null) return null;

            List<ulong> tmpResult = new List<ulong>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 8)
            {
                if (i + 7 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[8];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }
                    tmpResult.Add(BitConverter.ToUInt64(remainsVal, 0));
                }
                else
                {
                    tmpResult.Add(BitConverter.ToUInt64(m_OriginValueArray, i));
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get 32bit-real number array from origin values.
        /// </summary>
        /// <returns></returns>
        public float[] GetSingleArray()
        {
            if (m_OriginValueArray == null) return null;

            List<float> tmpResult = new List<float>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 4)
            {
                if (i + 3 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[4];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }
                    tmpResult.Add(BitConverter.ToSingle(remainsVal, 0));
                }
                else
                {
                    tmpResult.Add(BitConverter.ToSingle(m_OriginValueArray, i));
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get 64bit-real number array from origin values.
        /// </summary>
        /// <returns></returns>
        public double[] GetDoubleArray()
        {
            if (m_OriginValueArray == null) return null;

            List<double> tmpResult = new List<double>();
            for (int i = 0; i < m_OriginValueArray.Length; i += 8)
            {
                if (i + 7 >= m_OriginValueArray.Length)
                {
                    int remainCount = 0;
                    var remainsVal = new byte[8];
                    while (i < m_OriginValueArray.Length)
                    {
                        remainsVal[remainCount++] = m_OriginValueArray[i++];
                    }
                    tmpResult.Add(BitConverter.ToDouble(remainsVal, 0));
                }
                else
                {
                    tmpResult.Add(BitConverter.ToDouble(m_OriginValueArray, i));
                }
            }
            return tmpResult.ToArray();
        }
        /// <summary>
        /// Get ASCII string from origin values.
        /// </summary>
        /// <returns></returns>
        public string GetASCIIString()
        {
            if (m_OriginValueArray == null) return null;
            return Encoding.ASCII.GetString(m_OriginValueArray);
        }
        #endregion
    }


    /// <summary>
    /// Class related to a converter on PLC's generic data. 
    /// </summary>
    internal static class PLCConverter
    {
        #region Convert Data Methods

        #region Bit Units
        internal static string Convert1BitStringFromBooleanData(bool boolData)
        {
            return boolData ? "1" : "0";
        }
        internal static byte Convert1BitByteFromBooleanData(bool boolData)
        {
            return (byte)(boolData ? 0x10 : 0x00);
        }
        internal static string ConvertNBitStringFromBooleanArrayData(IEnumerable<bool> boolArr)
        {
            string tmpStr = string.Empty;
            foreach (var b in boolArr)
            {
                tmpStr += b ? "1" : "0";
            }
            return tmpStr;
        }
        internal static byte[] ConvertNBitByteArrayFromBooleanArrayData(IEnumerable<bool> boolArr)
        {
            int count = boolArr.Count();
            byte[] tmpByteArr = count % 2 == 0 ? new byte[count / 2] : new byte[count / 2 + 1];
            for (int i = 0; i < count; i += 2)
            {
                tmpByteArr[i / 2] = (byte)((boolArr.ElementAt(i) ? 0x10 : 0x00) + (i + 1 < count && boolArr.ElementAt(i + 1) ? 0x01 : 0x00));
            }
            return tmpByteArr;
        }
        #endregion

        #region Byte Units
        internal static byte Convert1ByteFromData(object val)
        {
            Type type = val.GetType();

            if (type == typeof(byte))
            {
                return (byte)val;
            }
            else if (type == typeof(char))
            {
                char charData = (char)val;
                return (byte)charData;
            }
            else
            {
                throw new Exception("Invalid data format.");
            }
        }
        internal static string Convert1ByteStringFromData(object val)
        {
            return (PLCConverter.Convert1ByteFromData(val)).ToString("X2");
        }
        #endregion

        #region Word Units
        internal static byte[] Convert1WordByteArrayFromData(object val)
        {
            Type type = val.GetType();

            if (type == typeof(byte))
            {
                byte byteData = (byte)val;
                return new byte[] { byteData, 0x00 };
            }
            else if (type == typeof(char))
            {
                char charData = (char)val;
                byte byteData = (byte)charData;
                return new byte[] { byteData, 0x00 };
            }
            else if (type == typeof(short))
            {
                short shortData = (short)val;
                return BitConverter.GetBytes(shortData);
            }
            else if (type == typeof(ushort))
            {
                ushort shortData = (ushort)val;
                return BitConverter.GetBytes(shortData);
            }
            else if (type == typeof(string) && (val as string).Length <= 2)
            {
                char[] charData = ((string)val).ToArray();
                if (charData.Length % 2 == 0) return Encoding.ASCII.GetBytes(charData);
                else
                {
                    byte[] tmpArr = new byte[charData.Length + 1];
                    var asciiData = Encoding.ASCII.GetBytes(charData);
                    for (int i = 0; i < asciiData.Length; i++) tmpArr[i] = asciiData[i];
                    return tmpArr;
                }
            }
            else if (val is IEnumerable<char> charArr && charArr.Count() <= 2)
            {
                char[] charData = charArr.ToArray();
                if (charData.Length % 2 == 0) return Encoding.ASCII.GetBytes(charData);
                else
                {
                    byte[] tmpArr = new byte[charData.Length + 1];
                    var asciiData = Encoding.ASCII.GetBytes(charData);
                    for (int i = 0; i < asciiData.Length; i++) tmpArr[i] = asciiData[i];
                    return tmpArr;
                }
            }
            else
            {
                throw new Exception("Invalid data format.");
            }
        }
        internal static  string Convert1WordStringFromData(object val)
        {
            return PLCConverter.ConvertValueToString(PLCConverter.Convert1WordByteArrayFromData(val));
        }

        #endregion

        #region Double Word Units
        internal static  byte[] Convert2WordsByteArrayFromData(object val)
        {
            Type type = val.GetType();

            if (type == typeof(int))
            {
                int intData = (int)val;
                return BitConverter.GetBytes(intData);
            }
            else if (type == typeof(uint))
            {
                uint intData = (uint)val;
                return BitConverter.GetBytes(intData);
            }
            else if (type == typeof(float))
            {
                float.TryParse(val.ToString(), out float floatData);
                return BitConverter.GetBytes(floatData);
            }
            else if (type == typeof(string) && (val as string).Length > 2 && (val as string).Length <= 4)
            {
                char[] charData = ((string)val).ToArray();
                if (charData.Length % 2 == 0) return Encoding.ASCII.GetBytes(charData);
                else
                {
                    byte[] tmpArr = new byte[charData.Length + 1];
                    var asciiData = Encoding.ASCII.GetBytes(charData);
                    for (int i = 0; i < asciiData.Length; i++) tmpArr[i] = asciiData[i];
                    return tmpArr;
                }
            }
            else if (val is IEnumerable<char> charArr && charArr.Count() > 2 && charArr.Count() <= 4)
            {
                char[] charData = charArr.ToArray();
                if (charData.Length % 2 == 0) return Encoding.ASCII.GetBytes(charData);
                else
                {
                    byte[] tmpArr = new byte[charData.Length + 1];
                    var asciiData = Encoding.ASCII.GetBytes(charData);
                    for (int i = 0; i < asciiData.Length; i++) tmpArr[i] = asciiData[i];
                    return tmpArr;
                }
            }
            else
            {
                throw new Exception("Invalid data format.");
            }
        }
        internal static  string Convert2WordsStringFromData(object val)
        {
            return PLCConverter.ConvertValueToString(PLCConverter.Convert2WordsByteArrayFromData(val), true);
        }

        internal static  byte[][] Convert2WordsByteArrayFrom4WordsData(object val)
        {
            Type type = val.GetType();
            byte[] tmpByteArray = null;
            if (type == typeof(long))
            {
                long longData = (long)val;
                tmpByteArray = BitConverter.GetBytes(longData);
            }
            else if (type == typeof(ulong))
            {
                ulong longData = (ulong)val;
                tmpByteArray = BitConverter.GetBytes(longData);
            }
            else if (type == typeof(double))
            {
                double doubleData = (double)val;
                tmpByteArray = BitConverter.GetBytes(doubleData);
            }
            else
            {
                throw new Exception("Invalid data format.");
            }
            return new byte[][] { tmpByteArray.Take(4).ToArray(), tmpByteArray.Skip(4).Take(4).ToArray() };
        }

        internal static  string[] Convert2WordsStringFrom4WordsData(object val)
        {
            var tmpArr = PLCConverter.Convert2WordsByteArrayFrom4WordsData(val);
            return new string[] { PLCConverter.ConvertValueToString(tmpArr[0], true), PLCConverter.ConvertValueToString(tmpArr[1], true) };
        }
        #endregion

        #region Multiple Units
        internal static  byte[] ConvertMultiWordsByteArrayFromData(object val)
        {
            Type type = val.GetType();

            if (type == typeof(byte))
            {
                byte byteData = (byte)val;
                return new byte[] { byteData, 0x00 };
            }
            else if (type == typeof(char))
            {
                char charData = (char)val;
                byte byteData = (byte)charData;
                return new byte[] { byteData, 0x00 };
            }
            else if (type == typeof(short))
            {
                short shortData = (short)val;
                return BitConverter.GetBytes(shortData);
            }
            else if (type == typeof(ushort))
            {
                ushort shortData = (ushort)val;
                return BitConverter.GetBytes(shortData);
            }
            else if (type == typeof(int))
            {
                int intData = (int)val;
                return BitConverter.GetBytes(intData);
            }
            else if (type == typeof(uint))
            {
                uint intData = (uint)val;
                return BitConverter.GetBytes(intData);
            }
            else if (type == typeof(long))
            {
                long longData = (long)val;
                return BitConverter.GetBytes(longData);
            }
            else if (type == typeof(ulong))
            {
                ulong longData = (ulong)val;
                return BitConverter.GetBytes(longData);
            }
            else if (type == typeof(float))
            {
                float floatData = (float)val;
                return BitConverter.GetBytes(floatData);
            }
            else if (type == typeof(double))
            {
                double doubleData = (double)val;
                return BitConverter.GetBytes(doubleData);
            }
            else if (type == typeof(string))
            {
                char[] charData = ((string)val).ToArray();
                if (charData.Length % 2 == 0) return Encoding.ASCII.GetBytes(charData);
                else
                {
                    byte[] tmpArr = new byte[charData.Length + 1];
                    var asciiData = Encoding.ASCII.GetBytes(charData);
                    for (int i = 0; i < asciiData.Length; i++) tmpArr[i] = asciiData[i];
                    return tmpArr;
                }
            }
            else if (val is IEnumerable<char> charData)
            {
                if (charData.Count() % 2 == 0) return Encoding.ASCII.GetBytes(charData.ToArray());
                else
                {
                    byte[] tmpArr = new byte[charData.Count() + 1];
                    var asciiData = Encoding.ASCII.GetBytes(charData.ToArray());
                    for (int i = 0; i < asciiData.Length; i++) tmpArr[i] = asciiData[i];
                    return tmpArr;
                }
            }
            else if (val is IEnumerable<byte> byteArrData)
            {
                if (byteArrData.Count() % 2 == 0) return byteArrData.ToArray();
                else
                {
                    byte[] tmpArr = new byte[byteArrData.Count() + 1];
                    var asciiData = byteArrData.ToArray();
                    for (int i = 0; i < asciiData.Length; i++) tmpArr[i] = asciiData[i];
                    return tmpArr;
                }
            }
            else
            {
                throw new Exception("Invalid data format.");
            }
        }
        internal static  byte[] ConvertMultiWordsByteArrayFromDataList(IEnumerable vals)
        {
            List<byte> tmpByteList = new List<byte>();
            foreach (var val in vals)
            {
                tmpByteList.AddRange(PLCConverter.ConvertMultiWordsByteArrayFromData(val));
            }
            return tmpByteList.ToArray();
        }

        internal static  string ConvertMultiWordsStringFromData(object val)
        {
            return PLCConverter.ConvertValueToString(PLCConverter.ConvertMultiWordsByteArrayFromData(val));
        }
        internal static  string ConvertMultiWordsStringFromDataList(IEnumerable vals)
        {
            var tmpStr = string.Empty;
            foreach (var val in vals)
            {
                tmpStr += PLCConverter.ConvertValueToString(PLCConverter.ConvertMultiWordsByteArrayFromData(val));
            }
            return tmpStr;
        }
        #endregion

        internal static  string ConvertValueToString(byte[] byteArray, bool isDWord = false)
        {
            string tmpStr = string.Empty;
            if (isDWord)
            {
                for (int i = 0; i < byteArray.Length; i += 4)
                {
                    tmpStr += (i + 3 < byteArray.Length ? byteArray[i + 3].ToString("X2") : "00")
                        + (i + 2 < byteArray.Length ? byteArray[i + 2].ToString("X2") : "00")
                        + (i + 1 < byteArray.Length ? byteArray[i + 1].ToString("X2") : "00")
                        + byteArray[i].ToString("X2");
                }
            }
            else
            {
                for (int i = 0; i < byteArray.Length; i += 2)
                {
                    tmpStr += (i + 1 < byteArray.Length ? byteArray[i + 1].ToString("X2") : "00") + byteArray[i].ToString("X2");
                }
            }
            return tmpStr;
        }

        internal static  byte[] ConvertHexStringToByteArray(string hexString)
        {
            return Enumerable.Range(0, hexString.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(hexString.Substring(x, 2), 16)).ToArray();
        }
        #endregion

        #region Convert Address Methods
        internal static  string ConvertStringFromAddress(PLCSendingPacket data)
        {
            var strCode = data.DeviceCode.ToString();
            string strAddr = string.Empty;
            if ((int)data.DeviceCode <= 0xA3 && (int)data.DeviceCode >= 0x9C) strAddr = data.Address.ToString("X6");
            else strAddr = data.Address.ToString("D6");

            return (strCode.Length == 1 ? strCode + "*" : strCode) + (strAddr.Length > 6 ? strAddr.Substring(strAddr.Length - 6, 6) : strAddr);
        }
        internal static  string ConvertStringFromAddress(PLCSendingPacket data, int offset)
        {
            var strCode = data.DeviceCode.ToString();
            string strAddr = string.Empty;
            if ((int)data.DeviceCode <= 0xA3 && (int)data.DeviceCode >= 0x9C) strAddr = (data.Address + offset).ToString("X6");
            else strAddr = (data.Address + offset).ToString("D6");

            return (strCode.Length == 1 ? strCode + "*" : strCode) + (strAddr.Length > 6 ? strAddr.Substring(strAddr.Length - 6, 6) : strAddr);
        }

        internal static  byte[] ConvertByteArrayFromAddress(PLCSendingPacket data)
        {
            byte[] tmpAddr = BitConverter.GetBytes(data.Address);
            tmpAddr[tmpAddr.Length - 1] = (byte)data.DeviceCode;
            return tmpAddr;
        }
        internal static  byte[] ConvertByteArrayFromAddress(PLCSendingPacket data, int offset)
        {
            byte[] tmpAddr = BitConverter.GetBytes(data.Address + offset);
            tmpAddr[tmpAddr.Length - 1] = (byte)data.DeviceCode;
            return tmpAddr;
        }
        #endregion

    }
}
