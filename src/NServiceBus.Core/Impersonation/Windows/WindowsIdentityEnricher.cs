namespace NServiceBus.Impersonation.Windows
{
    using System.Threading;
    using MessageMutator;
    using Unicast.Messages;

    /// <summary>
    /// Stamps outgoing messages with the current windows identity
    /// </summary>
    public class WindowsIdentityEnricher : IMutateOutgoingTransportMessages
    {
        public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
        {
            transportMessage.Headers[Headers.WindowsIdentityName] = Thread.CurrentPrincipal.Identity.Name;
        }
    }
}

