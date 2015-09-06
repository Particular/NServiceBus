namespace NServiceBus
{
    using System.Security.Principal;
    using System.Threading;
    using NServiceBus.MessageMutator;

    class WindowsIdentityEnricher : IMutateOutgoingTransportMessages
    {

        public void MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
            {
                context.Headers[Headers.WindowsIdentityName]= Thread.CurrentPrincipal.Identity.Name;
                return;
            }
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null)
            {
                context.Headers[Headers.WindowsIdentityName]= windowsIdentity.Name;
            }

        }
    }
}