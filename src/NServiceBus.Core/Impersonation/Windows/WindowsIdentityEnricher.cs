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

            if (transportMessage.Headers.ContainsKey(Headers.WindowsIdentityName))
                transportMessage.Headers.Remove(Headers.WindowsIdentityName);

            transportMessage.Headers.Add(Headers.WindowsIdentityName, Thread.CurrentPrincipal.Identity.Name);
        }
    }
}

