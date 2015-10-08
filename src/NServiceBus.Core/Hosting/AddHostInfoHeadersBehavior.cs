namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Hosting;
    using Pipeline;
    using Pipeline.Contexts;
    using Support;
    using TransportDispatch;

    class AddHostInfoHeadersBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public AddHostInfoHeadersBehavior(HostInformation hostInformation, EndpointName endpointName)
        {
            this.hostInformation = hostInformation;
            this.endpointName = endpointName;
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            context.SetHeader(Headers.OriginatingMachine, RuntimeEnvironment.MachineName);
            context.SetHeader(Headers.OriginatingEndpoint, endpointName.ToString());
            context.SetHeader(Headers.OriginatingHostId, hostInformation.HostId.ToString("N"));

            return next();
        }

        EndpointName endpointName;
        HostInformation hostInformation;
    }
}