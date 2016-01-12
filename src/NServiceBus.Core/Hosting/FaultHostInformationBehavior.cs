namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Support;

    class FaultHostInformationBehavior : Behavior<IFaultContext>
    {
        public FaultHostInformationBehavior(HostInformation hostInfo, EndpointName endpointName)
        {
            this.hostInfo = hostInfo;
            this.endpointName = endpointName;
        }

        public override Task Invoke(IFaultContext context, Func<Task> next)
        {
            context.AddFaultData(Headers.HostId, hostInfo.HostId.ToString("N"));
            context.AddFaultData(Headers.HostDisplayName, hostInfo.DisplayName);

            context.AddFaultData(Headers.ProcessingMachine, RuntimeEnvironment.MachineName);
            context.AddFaultData(Headers.ProcessingEndpoint, endpointName.ToString());

            return next();
        }

        HostInformation hostInfo;
        EndpointName endpointName;
    }
}