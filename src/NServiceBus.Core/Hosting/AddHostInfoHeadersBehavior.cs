namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;
    using NServiceBus.Support;

    class AddHostInfoHeadersBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        HostInformation hostInformation;
        Endpoint endpoint;

        public AddHostInfoHeadersBehavior(HostInformation hostInformation, Endpoint endpoint)
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
    }
}