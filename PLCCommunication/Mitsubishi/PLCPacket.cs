using System;
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
        /// Generate PLC data to write from this value & code & address.
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
        /// Generate PLC data received from this address & code.
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
}
