using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunication
{
    /// <summary>
    /// Enum related to protocol format to communicate.
    /// </summary>
    public enum EPLCProtocolFormat
    {
#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        Binary,
        ASCII
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
    }

    /// <summary>
    /// Interface related various PLC.
    /// </summary>
    public interface IPLC : IDisposable
    {
        /// <summary>
        /// Try to Connect PLC.
        /// </summary>
        void Connect();
        /// <summary>
        /// Disconnect PLC.
        /// </summary>
        void Disconnect();
        /// <summary>
        /// Refresh connection state.
        /// </summary>
        void Refresh();
        /// <summary>
        /// Load PLC settings in default path.
        /// </summary>
        void Load();
        /// <summary>
        /// Load PLC settings in specific path.
        /// </summary>
        /// <param name="filePath">Path to load.</param>
        void Load(string filePath);
        /// <summary>
        /// Save PLC settings in default path.
        /// </summary>
        void Save();
        /// <summary>
        /// Save PLC settings in specific path.
        /// </summary>
        /// <param name="filePath">Path to save</param>
        void Save(string filePath);
    }
}
