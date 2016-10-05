namespace NServiceBus.Impersonation.Windows
{
    using System.Security.Principal;
    using System.Threading;
    using NServiceBus.MessageMutator;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    ///     Stamps outgoing messages with the current windows identity
    /// </summary>
    class WindowsIdentityEnricher : IMutateOutgoingTransportMessages
    {
        public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
        {
            if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
            {
                transportMessage.Headers[Headers.WindowsIdentityName] = Thread.CurrentPrincipal.Identity.Name;
                return;
            }
            var windowsIdentity = WindowsIdentity.GetCurrent();
            transportMessage.Headers[Headers.WindowsIdentityName] = windowsIdentity.Name;
        }
    }
}