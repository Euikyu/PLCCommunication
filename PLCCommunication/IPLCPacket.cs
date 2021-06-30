using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication
{
    public interface IPLCSendingPacket
    {
        int Address { get; }
        object Value { get; }
    }
    public interface IPLCReceivingPacket
    {
        bool[] GetBooleanArray();
        byte[] GetByteArray();
        short[] GetInt16Array();
        ushort[] GetUInt16Array();
        int[] GetInt32Array();
        uint[] GetUInt32Array();
        long[] GetInt64Array();
        ulong[] GetUInt64Array();
        float[] GetSingleArray();
        double[] GetDoubleArray();
        string GetASCIIString();
    }
}
