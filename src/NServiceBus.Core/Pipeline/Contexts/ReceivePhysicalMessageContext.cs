namespace NServiceBus.Pipeline.Contexts
{
    using System.ComponentModel;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ReceivePhysicalMessageContext : BehaviorContext
    {
        public ReceivePhysicalMessageContext(BehaviorContext parentContext, TransportMessage transportMessage, bool messageHandlingDisabled)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;

            Set(IncomingPhysicalMessageKey, transportMessage);
            Set("MessageHandlingDisabled", messageHandlingDisabled);
        }

        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(IncomingPhysicalMessageKey); }
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