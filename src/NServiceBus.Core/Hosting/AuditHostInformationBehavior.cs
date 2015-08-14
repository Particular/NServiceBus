namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Audit;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Support;

    class AuditHostInformationBehavior : Behavior<AuditContext>
    {
        public AuditHostInformationBehavior(HostInformation hostInfo,string endpointName)
        {
            this.hostInfo = hostInfo;
            this.endpointName = endpointName;
        }

        public override Task Invoke(AuditContext context, Func<Task> next)
        {
            context.AddAuditData(Headers.HostId, hostInfo.HostId.ToString("N"));
            context.AddAuditData(Headers.HostDisplayName,hostInfo.DisplayName);

            context.AddAuditData(Headers.ProcessingMachine,RuntimeEnvironment.MachineName);
            context.AddAuditData(Headers.ProcessingEndpoint,endpointName);

            return next();
        }

        readonly HostInformation hostInfo;
        readonly string endpointName;
    }
}