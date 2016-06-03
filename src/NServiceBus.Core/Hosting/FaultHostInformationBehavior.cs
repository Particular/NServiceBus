namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Hosting;
    using Pipeline;
    using Support;

    class FaultHostInformationBehavior : Behavior<IFaultContext>
    {
        public FaultHostInformationBehavior(HostInformation hostInfo, string endpointName)
        {
            this.hostInfo = hostInfo;
            this.endpointName = endpointName;
        }

        public override Task Invoke(IFaultContext context, Func<Task> next)
        {
            context.AddFaultData(Headers.HostId, hostInfo.HostId.ToString("N"));
            context.AddFaultData(Headers.HostDisplayName, hostInfo.DisplayName);

            context.AddFaultData(Headers.ProcessingMachine, RuntimeEnvironment.MachineName);
            context.AddFaultData(Headers.ProcessingEndpoint, endpointName);

            return next();
        }

        string endpointName;

        HostInformation hostInfo;
    }
}