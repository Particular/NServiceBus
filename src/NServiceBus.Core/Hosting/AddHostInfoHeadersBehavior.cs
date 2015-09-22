namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Support;
    using NServiceBus.TransportDispatch;

    class AddHostInfoHeadersBehavior : Behavior<OutgoingContext>
    {
        HostInformation hostInformation;
        EndpointName endpointName;

        public AddHostInfoHeadersBehavior(HostInformation hostInformation, EndpointName endpointName)
        {
            this.hostInformation = hostInformation;
            this.endpointName = endpointName;
        }

        public override Task Invoke(OutgoingContext context, Func<Task> next)
        {
            context.SetHeader(Headers.OriginatingMachine, RuntimeEnvironment.MachineName);
            context.SetHeader(Headers.OriginatingEndpoint, endpointName.ToString());
            context.SetHeader(Headers.OriginatingHostId, hostInformation.HostId.ToString("N"));

            return next();
        }
    }
}