using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication
{
    /// <summary>
    /// Interface related to PLC data to send.
    /// </summary>
    public interface IPLCSendingPacket
    {
        /// <summary>
        /// Address of PLC to send.
        /// </summary>
        int Address { get; }
        /// <summary>
        /// Values to send.
        /// </summary>
        object Value { get; }
    }
    /// <summary>
    /// Interface related to PLC data to receive.
    /// </summary>
    public interface IPLCReceivingPacket
    {
        /// <summary>
        /// Get boolean array from origin values.
        /// </summary>
        /// <returns></returns>
        bool[] GetBooleanArray();
        /// <summary>
        /// Get byte array from origin values.
        /// </summary>
        /// <returns></returns>
        byte[] GetByteArray();
        /// <summary>
        /// Get 16bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        short[] GetInt16Array();
        /// <summary>
        /// Get unsigned 16bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        ushort[] GetUInt16Array();
        /// <summary>
        /// Get 32bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        int[] GetInt32Array();
        /// <summary>
        /// Get unsigned 32bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        uint[] GetUInt32Array();
        /// <summary>
        /// Get 64bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        long[] GetInt64Array();
        /// <summary>
        /// Get unsigned 64bit-integer array from origin values.
        /// </summary>
        /// <returns></returns>
        ulong[] GetUInt64Array();
        /// <summary>
        /// Get 32bit-real number array from origin values.
        /// </summary>
        /// <returns></returns>
        float[] GetSingleArray();
        /// <summary>
        /// Get 64bit-real number array from origin values.
        /// </summary>
        /// <returns></returns>
        double[] GetDoubleArray();
        /// <summary>
        /// Get ASCII string from origin values.
        /// </summary>
        /// <returns></returns>
        string GetASCIIString();
    }
}
