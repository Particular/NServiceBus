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
        public ReceivePhysicalMessageContext(BehaviorContext parentContext, TransportMessage transportMessage, bool messageHandlingDisabled)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;

            Set(IncomingPhysicalMessageKey, transportMessage);
            Set("MessageHandlingDisabled", messageHandlingDisabled);

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

        public bool MessageHandlingDisabled
        {
            get { return Get<bool>("MessageHandlingDisabled"); }
        }

        public static string IncomingPhysicalMessageKey
        {
            get { return "NServiceBus.IncomingPhysicalMessage"; }
        }
    }
}