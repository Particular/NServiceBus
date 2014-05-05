namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Unicast.Messages;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ReceivePhysicalMessageContext : BehaviorContext
    {
        public ReceivePhysicalMessageContext(BehaviorContext parentContext, TransportMessage transportMessage)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;

            Set(IncomingPhysicalMessageKey, transportMessage);

            LogicalMessages = new List<LogicalMessage>();
        }

        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(IncomingPhysicalMessageKey); }
        }
        public List<LogicalMessage> LogicalMessages
        {
            get { return Get<List<LogicalMessage>>(); }
            set { Set(value); }
        }

        public static string IncomingPhysicalMessageKey
        {
            get { return "NServiceBus.IncomingPhysicalMessage"; }
        }
    }
}