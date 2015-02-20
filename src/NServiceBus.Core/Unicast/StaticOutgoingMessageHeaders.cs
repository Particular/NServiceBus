namespace NServiceBus.Unicast
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public class StaticOutgoingMessageHeaders
    {
        ConcurrentDictionary<string, string> staticOutgoingHeaders = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders
        {
            get { return staticOutgoingHeaders; }
        }
    }
}