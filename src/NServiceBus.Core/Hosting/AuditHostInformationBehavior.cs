namespace NServiceBus
{
    using System;
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

        public override void Invoke(AuditContext context, Action next)
        {
            context.AddAuditData(Headers.HostId, hostInfo.HostId.ToString("N"));
            context.AddAuditData(Headers.HostDisplayName,hostInfo.DisplayName);

            context.AddAuditData(Headers.ProcessingMachine,RuntimeEnvironment.MachineName);
            context.AddAuditData(Headers.ProcessingEndpoint,endpointName);

            next();
        }

        readonly HostInformation hostInfo;
        readonly string endpointName;
    }
}