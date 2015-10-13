namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Pipeline.Contexts;
    using TransportDispatch;

    class AddWindowsIdentityHeaderBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public AddWindowsIdentityHeaderBehavior(string identity)
        {
            this.identity = identity;
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            context.SetHeader(Headers.WindowsIdentityName, identity);
            return next();
        }

        string identity;
    }
}