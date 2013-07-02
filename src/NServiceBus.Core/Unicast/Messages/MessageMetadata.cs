namespace NServiceBus.Unicast.Messages
{
    using System;

    public class MessageMetadata
    {
        public Type MessageType { get; set; }
        public bool Recoverable { get; set; }
        public TimeSpan TimeToBeReceived { get; set; }

        public override string ToString()
        {
            return string.Format("MessageType: {0}, Recoverable: {1}, TimeToBeReceived: {2}", MessageType, Recoverable,
                                 TimeToBeReceived == TimeSpan.MaxValue ? "Not set" : TimeToBeReceived.ToString());
        }
    }
}