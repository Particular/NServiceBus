namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Support;

    class AddHostInfoHeadersBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        HostInformation hostInformation;
        EndpointName endpointName;

        public AddHostInfoHeadersBehavior(HostInformation hostInformation, EndpointName endpointName)
        {
            this.hostInformation = hostInformation;
            this.endpointName = endpointName;
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            context.Headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
            context.Headers[Headers.OriginatingEndpoint] = endpointName.ToString();
            context.Headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");

            return next();
        }
    }
}