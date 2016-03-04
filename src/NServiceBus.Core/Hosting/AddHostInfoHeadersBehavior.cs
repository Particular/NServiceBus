namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Hosting;
    using Pipeline;
    using Routing;
    using Support;

    class AddHostInfoHeadersBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public AddHostInfoHeadersBehavior(HostInformation hostInformation, EndpointName endpoint)
        {
            this.hostInformation = hostInformation;
            this.endpoint = endpoint;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            context.Headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
            context.Headers[Headers.OriginatingEndpoint] = endpoint.ToString();
            context.Headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");

            return next();
        }

        EndpointName endpoint;
        HostInformation hostInformation;
    }
}