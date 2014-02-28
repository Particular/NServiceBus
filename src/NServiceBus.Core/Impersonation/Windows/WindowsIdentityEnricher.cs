namespace NServiceBus.Impersonation.Windows
{
    using System.Threading;
    using MessageMutator;

    /// <summary>
    /// Stamps outgoing messages with the current windows identity
    /// </summary>
    public class WindowsIdentityEnricher : IMutateOutgoingTransportMessages
    {
        void IMutateOutgoingTransportMessages.MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers[Headers.WindowsIdentityName] = Thread.CurrentPrincipal.Identity.Name;
        }
    }
}

