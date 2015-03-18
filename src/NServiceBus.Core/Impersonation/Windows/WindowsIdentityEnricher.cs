namespace NServiceBus.Impersonation.Windows
{
    using System.Security.Principal;
    using System.Threading;

    class WindowsIdentityEnricher : IMutateOutgoingPhysicalContext
    {

        public void MutateOutgoing(OutgoingPhysicalMutatorContext context)
        {
            if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
            {
                context.Headers[Headers.WindowsIdentityName] = Thread.CurrentPrincipal.Identity.Name;
                return;
            }
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null)
            {
                context.Headers[Headers.WindowsIdentityName] = windowsIdentity.Name;
            }

        }
    }
}