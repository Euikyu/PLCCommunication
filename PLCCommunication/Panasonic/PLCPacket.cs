using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication.Panasonic
{
    public enum EPLCDeviceCode
    {
        //Use Binary
        /// <summary>
        /// Link relay contact. (only use binary)
        /// </summary>
        WL = 0,
        /// <summary>
        /// Internal relay contact. (only use binary)
        /// </summary>
        WR = 1,
        /// <summary>
        /// External output relay contact. (only use binary)
        /// </summary>
        WY = 2,
        /// <summary>
        /// External input relay contact. (only use binary)
        /// </summary>
        WX = 3,
        /// <summary>
        /// Timer/Counter setting value. (only use binary)
        /// </summary>
        SV = 4,
        /// <summary>
        /// Timer/Counter Progress value. (only use binary)
        /// </summary>
        EV = 5,
        /// <summary>
        /// Link register. (only use binary)
        /// </summary>
        LD = 6,
        /// <summary>
        /// Special relay contact. (only use binary)
        /// </summary>
        S_WR = 7,
        /// <summary>
        /// Special data register. (only use binary)
        /// </summary>
        S_DT = 8,
        /// <summary>
        /// Data register. (only use binary)
        /// </summary>
        DT = 9,
        /// <summary>
        /// File register. (only use binary)
        /// </summary>
        FL = 10,

        //Use ASCII Contact (Read-only)
        /// <summary>
        /// Timer value. (only use ASCII and read-only)
        /// </summary>
        T = 100,
        /// <summary>
        /// Counter value. (only use ASCII and read-only)
        /// </summary>
        C = 101,
        /// <summary>
        /// External input contact. (only use ASCII and read-only)
        /// </summary>
        X = 102,

        //Read-write both
        /// <summary>
        /// External output contact. (only use ASCII)
        /// </summary>
        Y = 150,
        /// <summary>
        /// Internal relay contact. (only use ASCII)
        /// </summary>
        R = 151,
        /// <summary>
        /// Link relay contact. (only use ASCII)
        /// </summary>
        C_L = 152,

        //Use ASCII Data
        /// <summary>
        /// Link register. (only use ASCII)
        /// </summary>
        L = 200,
        /// <summary>
        /// Data register. (only use ASCII)
        /// </summary>
        D = 201,
        /// <summary>
        /// File register. (only use ASCII)
        /// </summary>
        F = 202,

        //Use ASCII index register
        /// <summary>
        /// Index 0 register. (only use ASCII)
        /// </summary>
        IX = 300,
        /// <summary>
        /// Index 1 register. (only use ASCII)
        /// </summary>
        IY = 301,
        /// <summary>
        /// Index 0, 1 both register. (only use ASCII)
        /// </summary>
        ID = 302,
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
        /// Address of contact.
        /// </summary>
        public string ContactAddress { get; }
        /// <summary>
        /// Value to write.
        /// </summary>
        public object Value { get; }
        /// <summary>
        /// Data to Determine a contact or not.
        /// </summary>
        public bool IsContact { get; }
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
        public PLCSendingPacket(EPLCDeviceCode code, string address, bool isContact, bool isRead, ushort readWordCount)
        {
            this.DeviceCode = code;
            this.IsContact = isContact;
            if (isContact)
            {
                if (int.TryParse(address.Substring(0, address.Length - 1), out int num) && int.TryParse(address.Substring(address.Length - 1, 1), System.Globalization.NumberStyles.HexNumber, null, out int hex))
                {
                    string res = string.Empty;
                    if (address.Length < 4)
                    {
                        res = "0000" + address;
                    }
                    this.ContactAddress = res.Substring(res.Length - 4, 4);
                    this.Address = num;
                }
                else
                {
                    this.ContactAddress = "0000";
                    this.Address = 0;
                }
            }
            else
            {
                if (int.TryParse(address, out int num))
                {
                    this.Address = num;
                    this.ContactAddress = address;
                }
                else
                {
                    this.Address = 0;
                    ContactAddress = "0000";
                }
            }
            this.Value = byte.MinValue;
            this.WordCount = readWordCount;
            this.IsRead = true;
        }

        /// <summary>
        /// Generate PLC data to write from this value & code & address.
        /// </summary>
        /// <param name="code">PLC device code to use.</param>
        /// <param name="address">Address to use.</param>
        /// <param name="value">Value to write.</param>
        public PLCSendingPacket(EPLCDeviceCode code, string address, bool isContact, object value)
        {
            this.DeviceCode = code;
            this.IsContact = isContact;
            if (isContact)
            {
                if (int.TryParse(address.Substring(0, address.Length - 1), out int num) && int.TryParse(address.Substring(address.Length - 1, 1), System.Globalization.NumberStyles.HexNumber, null, out int hex))
                {
                    string res = string.Empty;
                    if (address.Length < 4)
                    {
                        res = "0000" + address;
                    }
                    this.ContactAddress = res.Substring(res.Length - 4, 4);
                    this.Address = num;
                }
                else
                {
                    this.ContactAddress = "0000";
                    this.Address = 0;
                }
            }
            else
            {
                if (int.TryParse(address, out int num))
                {
                    this.Address = num;
                    this.ContactAddress = address;
                }
                else
                {
                    this.Address = 0;
                    ContactAddress = "0000";
                }
            }
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
        public string ContactAddress { get; }
        #endregion

        /// <summary>
        /// Generate PLC data received from this address & code.
        /// </summary>
        /// <param name="receivedData">Received data from PLC.</param>
        /// <param name="code">Data's PLC device code.</param>
        /// <param name="address">data's first address.</param>
        public PLCReceivingPacket(byte[] receivedData, EPLCDeviceCode code, string address)
        {
            this.m_OriginValueArray = receivedData.ToArray();
            this.DeviceCode = code;

            if (int.TryParse(address, out int num0))
            {
                this.Address = num0;
                this.ContactAddress = address;
            }
            else if (int.TryParse(address.Substring(0, address.Length - 1), out int num1) && num1 < 1000 && int.TryParse(address.Substring(address.Length - 1, 1), System.Globalization.NumberStyles.HexNumber, null, out int hex))
            {
                string res = string.Empty;
                if (address.Length < 4)
                {
                    res = "0000" + address;
                }
                this.ContactAddress = res.Substring(res.Length - 4, 4);
                this.Address = (num1 % 1000) * 10 + hex;
            }
            else
            {
                this.ContactAddress = "0000";
                this.Address = 0;
            }
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

    internal static class PLCConverter
    {
        #region Convert Data Methods

        #region Calc methods
        internal static int CalcWordCount(object val)
        {
            var byteCount = Marshal.SizeOf(val);
            if (byteCount == 0) return 0;
            return byteCount % 2 == 0 ? byteCount / 2 : byteCount / 2 + 1;
        }
        internal static string EncodeBCC(string msg)
        {
            byte tmpBCC = byte.MinValue;

            foreach(var c in msg)
            {
                tmpBCC = (byte)(tmpBCC ^ (byte)c);
            }

            return tmpBCC.ToString("X2");
        }
        internal static bool DecodeBCC(string msg, string bcc)
        {
            byte tmpBCC = byte.MinValue;

            foreach (var c in msg)
            {
                tmpBCC = (byte)(tmpBCC ^ (byte)c);
            }

            return tmpBCC.ToString("X2").Equals(bcc);
        }
        #endregion

        #region Bit Units
        internal static string Convert1BitStringFromBooleanData(bool boolData)
        {
            return boolData ? "1" : "0";
        }
        internal static byte Convert1BitByteFromBooleanData(bool boolData)
        {
            return (byte)(boolData ? 0x01 : 0x00);
        }
        #endregion
        
        #region Multiple Units
        internal static string ConvertNWordsStringFromBooleanArrayData(IEnumerable<bool> boolArr)
        {
            string tmpStr = string.Empty;
            for (int i = 0; i < boolArr.Count(); i += 16)
            {
                byte tmpByte0 = 0, tmpByte1 = 0;

                tmpByte0 += (byte)(boolArr.Count() < i + 0x00 ? (boolArr.ElementAt(i + 0x00) ? 0x01 : 0x00) : 0x00);
                tmpByte0 += (byte)(boolArr.Count() < i + 0x01 ? (boolArr.ElementAt(i + 0x01) ? 0x02 : 0x00) : 0x00);
                tmpByte0 += (byte)(boolArr.Count() < i + 0x02 ? (boolArr.ElementAt(i + 0x02) ? 0x04 : 0x00) : 0x00);
                tmpByte0 += (byte)(boolArr.Count() < i + 0x03 ? (boolArr.ElementAt(i + 0x03) ? 0x08 : 0x00) : 0x00);
                tmpByte0 += (byte)(boolArr.Count() < i + 0x04 ? (boolArr.ElementAt(i + 0x04) ? 0x10 : 0x00) : 0x00);
                tmpByte0 += (byte)(boolArr.Count() < i + 0x05 ? (boolArr.ElementAt(i + 0x05) ? 0x20 : 0x00) : 0x00);
                tmpByte0 += (byte)(boolArr.Count() < i + 0x06 ? (boolArr.ElementAt(i + 0x06) ? 0x40 : 0x00) : 0x00);
                tmpByte0 += (byte)(boolArr.Count() < i + 0x07 ? (boolArr.ElementAt(i + 0x07) ? 0x80 : 0x00) : 0x00);

                tmpByte1 += (byte)(boolArr.Count() < i + 0x08 ? (boolArr.ElementAt(i + 0x08) ? 0x01 : 0x00) : 0x00);
                tmpByte1 += (byte)(boolArr.Count() < i + 0x09 ? (boolArr.ElementAt(i + 0x09) ? 0x02 : 0x00) : 0x00);
                tmpByte1 += (byte)(boolArr.Count() < i + 0x0A ? (boolArr.ElementAt(i + 0x0A) ? 0x04 : 0x00) : 0x00);
                tmpByte1 += (byte)(boolArr.Count() < i + 0x0B ? (boolArr.ElementAt(i + 0x0B) ? 0x08 : 0x00) : 0x00);
                tmpByte1 += (byte)(boolArr.Count() < i + 0x0C ? (boolArr.ElementAt(i + 0x0C) ? 0x10 : 0x00) : 0x00);
                tmpByte1 += (byte)(boolArr.Count() < i + 0x0D ? (boolArr.ElementAt(i + 0x0D) ? 0x20 : 0x00) : 0x00);
                tmpByte1 += (byte)(boolArr.Count() < i + 0x0E ? (boolArr.ElementAt(i + 0x0E) ? 0x40 : 0x00) : 0x00);
                tmpByte1 += (byte)(boolArr.Count() < i + 0x0F ? (boolArr.ElementAt(i + 0x0F) ? 0x80 : 0x00) : 0x00);

                tmpStr += tmpByte0.ToString("X2") + tmpByte1.ToString("X2");
            }

            if (boolArr.Count() <= 16)
            {
                tmpStr += tmpStr;
            }

            return tmpStr;
        }
        internal static byte[] ConvertNWordsByteArrayFromBooleanArrayData(IEnumerable<bool> boolArr)
        {
            int count = boolArr.Count();
            var byteList = new List<byte>();
            for (int i = 0; i < count; i += 16)
            {
                byte tmpByte0 = 0, tmpByte1 = 0;

                tmpByte0 += (byte)(count < i + 0x00 ? (boolArr.ElementAt(i + 0x00) ? 0x01 : 0x00) : 0x00);
                tmpByte0 += (byte)(count < i + 0x01 ? (boolArr.ElementAt(i + 0x01) ? 0x02 : 0x00) : 0x00);
                tmpByte0 += (byte)(count < i + 0x02 ? (boolArr.ElementAt(i + 0x02) ? 0x04 : 0x00) : 0x00);
                tmpByte0 += (byte)(count < i + 0x03 ? (boolArr.ElementAt(i + 0x03) ? 0x08 : 0x00) : 0x00);
                tmpByte0 += (byte)(count < i + 0x04 ? (boolArr.ElementAt(i + 0x04) ? 0x10 : 0x00) : 0x00);
                tmpByte0 += (byte)(count < i + 0x05 ? (boolArr.ElementAt(i + 0x05) ? 0x20 : 0x00) : 0x00);
                tmpByte0 += (byte)(count < i + 0x06 ? (boolArr.ElementAt(i + 0x06) ? 0x40 : 0x00) : 0x00);
                tmpByte0 += (byte)(count < i + 0x07 ? (boolArr.ElementAt(i + 0x07) ? 0x80 : 0x00) : 0x00);

                tmpByte1 += (byte)(count < i + 0x08 ? (boolArr.ElementAt(i + 0x08) ? 0x01 : 0x00) : 0x00);
                tmpByte1 += (byte)(count < i + 0x09 ? (boolArr.ElementAt(i + 0x09) ? 0x02 : 0x00) : 0x00);
                tmpByte1 += (byte)(count < i + 0x0A ? (boolArr.ElementAt(i + 0x0A) ? 0x04 : 0x00) : 0x00);
                tmpByte1 += (byte)(count < i + 0x0B ? (boolArr.ElementAt(i + 0x0B) ? 0x08 : 0x00) : 0x00);
                tmpByte1 += (byte)(count < i + 0x0C ? (boolArr.ElementAt(i + 0x0C) ? 0x10 : 0x00) : 0x00);
                tmpByte1 += (byte)(count < i + 0x0D ? (boolArr.ElementAt(i + 0x0D) ? 0x20 : 0x00) : 0x00);
                tmpByte1 += (byte)(count < i + 0x0E ? (boolArr.ElementAt(i + 0x0E) ? 0x40 : 0x00) : 0x00);
                tmpByte1 += (byte)(count < i + 0x0F ? (boolArr.ElementAt(i + 0x0F) ? 0x80 : 0x00) : 0x00);

                byteList.Add(tmpByte0);
                byteList.Add(tmpByte1);
            }

            return byteList.ToArray();
        }
        internal static byte[] ConvertMultiWordsByteArrayFromData(object val)
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
        internal static byte[] ConvertMultiWordsByteArrayFromDataList(IEnumerable vals)
        {
            List<byte> tmpByteList = new List<byte>();
            foreach (var val in vals)
            {
                tmpByteList.AddRange(PLCConverter.ConvertMultiWordsByteArrayFromData(val));
            }
            return tmpByteList.ToArray();
        }

        internal static string ConvertMultiWordsStringFromData(object val)
        {
            return PLCConverter.ConvertValueToString(PLCConverter.ConvertMultiWordsByteArrayFromData(val));
        }
        internal static string ConvertMultiWordsStringFromDataList(IEnumerable vals)
        {
            var tmpStr = string.Empty;
            foreach (var val in vals)
            {
                tmpStr += PLCConverter.ConvertValueToString(PLCConverter.ConvertMultiWordsByteArrayFromData(val));
            }
            return tmpStr;
        }
        #endregion

        internal static string ConvertValueToString(byte[] byteArray)
        {
            string tmpStr = string.Empty;

            for (int i = 0; i < byteArray.Length; i++)
            {
                tmpStr += byteArray[i].ToString("X2");
            }
            return tmpStr;
        }

        internal static byte[] ConvertHexStringToByteArray(string hexString)
        {
            return Enumerable.Range(0, hexString.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(hexString.Substring(x, 2), 16)).ToArray();
        }
        #endregion

        #region Convert Address Methods
        internal static string ConvertStringFromAddress(PLCSendingPacket data)
        {
            if ((int)data.DeviceCode < (int)EPLCDeviceCode.T ||((int)data.DeviceCode > (int)EPLCDeviceCode.C_L && (int)data.DeviceCode < (int)EPLCDeviceCode.IX)) throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());

            var strCode = data.DeviceCode.ToString().Contains("_") ? data.DeviceCode.ToString().Split('_').Last() : data.DeviceCode.ToString();

            string strAddr = string.Empty;
            if ((int)data.DeviceCode < (int)EPLCDeviceCode.L)
            {
                if (data.IsContact)
                {
                    strAddr = data.ContactAddress;
                    strAddr = strAddr.Substring(strAddr.Length - 4, 4);
                }
                else
                {
                    strAddr = data.Address.ToString("D4");
                    strAddr = strAddr.Substring(strAddr.Length - 4, 4);
                }
            }
            else
            {
                strAddr = "000000000";
            }

            return strCode + strAddr;
        }
        internal static string ConvertStringFromAddress(PLCSendingPacket data, int offset)
        {
            if ((int)data.DeviceCode < (int)EPLCDeviceCode.T || (int)data.DeviceCode >= (int)EPLCDeviceCode.IX) throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            var strCode = data.DeviceCode.ToString().Contains("_") ? data.DeviceCode.ToString().Split('_').Last() : data.DeviceCode.ToString();

            string strAddr = string.Empty;
            if ((int)data.DeviceCode < (int)EPLCDeviceCode.L)
            {
                strAddr = data.IsContact ? data.ContactAddress : data.Address.ToString("D4");
                strAddr = strAddr.Substring(strAddr.Length - 4, 4);
            }
            else
            {
                if (offset <= 0) throw new Exception("Invalid offset value.");
                var startAddr = data.Address.ToString("D5");
                startAddr = startAddr.Substring(startAddr.Length - 5, 5);
                var endAddr = (data.Address + offset - 1).ToString("D5");
                endAddr = endAddr.Substring(endAddr.Length - 5, 5);
                strAddr = startAddr + endAddr;
            }

            return strCode + strAddr;
        }

        internal static byte[] ConvertByteArrayFromContactAddress(PLCSendingPacket data)
        {
            if ((int)data.DeviceCode >= (int)EPLCDeviceCode.T) throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            
            List<byte> tmpAddr = new List<byte>
            {
                (byte)data.DeviceCode
            };
            tmpAddr.AddRange(BitConverter.GetBytes(data.Address).Take(2));
            tmpAddr.Add(byte.TryParse(data.ContactAddress.Substring(data.ContactAddress.Length - 1, 1), out byte b) ? b : (byte)0);
            return tmpAddr.ToArray();
        }
        internal static byte[] ConvertByteArrayFromDataAddress(PLCSendingPacket data, int offset)
        {
            if ((int)data.DeviceCode >= (int)EPLCDeviceCode.T) throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            if (offset <= 0) throw new Exception("Invalid offset value.");

            List<byte> tmpAddr = new List<byte>
            {
                (byte)data.DeviceCode
            };
            tmpAddr.AddRange(BitConverter.GetBytes(data.Address).Take(2));
            tmpAddr.AddRange(BitConverter.GetBytes((short)offset));
            return tmpAddr.ToArray();
        }
        #endregion

    }
}
