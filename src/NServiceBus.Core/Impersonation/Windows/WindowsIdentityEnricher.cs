namespace NServiceBus.Impersonation.Windows
{
    using System.Threading;
    using MessageMutator;
    using Unicast.Messages;

    /// <summary>
    /// Stamps outgoing messages with the current windows identity
    /// </summary>
    class WindowsIdentityEnricher : IMutateOutgoingTransportMessages
    {
        public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
        {
            if (Thread.CurrentPrincipal != null)
            {
                transportMessage.Headers[Headers.WindowsIdentityName] = Thread.CurrentPrincipal.Identity.Name;
            }
        }
    }
}

