using System;
using System.IO;

namespace NServiceBus.Unicast.Queuing
{
    public class QueuedMessage
    {
        public string CorrelationId { get; set;  }
        public bool Recoverable { get; set; }
        public string ResponseQueue { get; set; }
        public string Label { get; set; }
        public TimeSpan TimeToBeReceived { get; set; }
        public DateTime TimeSent { get; set; }
        public int AppSpecific { get; set; }
        public long LookupId { get; set; }

        public Stream BodyStream { get; set; }
        public byte[] Extension { get; set; }

        /// <summary>
        /// Set by the transport
        /// </summary>
        public string Id { get; set;}
    }
}
