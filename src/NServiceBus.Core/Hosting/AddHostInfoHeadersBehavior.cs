namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Hosting;
    using Pipeline;
    using Support;

    class AddHostInfoHeadersBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public AddHostInfoHeadersBehavior(HostInformation hostInformation, EndpointInfo endpoint)
        {
            this.hostInformation = hostInformation;
            this.endpoint = endpoint;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            if (!context.Headers.ContainsKey(Headers.NServiceBusVersion))
            {
                context.Headers[Headers.NServiceBusVersion] = endpoint.NServiceBusVersion;
            }

            context.Headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
            context.Headers[Headers.OriginatingEndpoint] = endpoint.Name;
            context.Headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");

            return next(context);
        }

        EndpointInfo endpoint;
        HostInformation hostInformation;
    }
}