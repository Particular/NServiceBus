namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MessageMetadata
    {
        public Type MessageType { get; set; }
        public bool Recoverable { get; set; }
        public TimeSpan TimeToBeReceived { get; set; }
        public IEnumerable<Type> MessageHierarchy{ get; set; }


        public override string ToString()
        {
            return string.Format("MessageType: {0}, Recoverable: {1}, TimeToBeReceived: {2} , Parent types: {3}", MessageType, Recoverable,
                                 TimeToBeReceived == TimeSpan.MaxValue ? "Not set" : TimeToBeReceived.ToString(),string.Join(";",MessageHierarchy.Select(pt=>pt.FullName)));
        }
    }
}