namespace NServiceBus
{
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using MessageMutator;

    class WindowsIdentityEnricher : IMutateOutgoingTransportMessages
    {
        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
            {
                context.OutgoingHeaders[Headers.WindowsIdentityName] = Thread.CurrentPrincipal.Identity.Name;
                return TaskEx.Completed;
            }
            using (var windowsIdentity = WindowsIdentity.GetCurrent())
            {
                if (windowsIdentity != null)
                {
                    context.OutgoingHeaders[Headers.WindowsIdentityName] = windowsIdentity.Name;
                }
            }
            return TaskEx.Completed;
        }
    }
}