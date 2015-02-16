namespace NServiceBus
{
    using System;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class HostInformationBehavior : IBehavior<IncomingContext>
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public HostInformation Host { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            context.PhysicalMessage.Headers[Headers.HostId] = Host.HostId.ToString("N");
            context.PhysicalMessage.Headers[Headers.HostDisplayName] = Host.DisplayName;

            next();
        }
    }
}