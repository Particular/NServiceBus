using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NServiceBus.Utils
{
    /// <summary>
    /// Represents the structure of header information passed in a TransportMessage.
    /// </summary>
    [Serializable]
    public class HeaderInfo
    {
        /// <summary>
        /// The key used to lookup the value in the header collection.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The value stored under the key in the header collection.
        /// </summary>
        public string Value { get; set; }
    }
}
