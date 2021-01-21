namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Hosting;
    using Pipeline;
    using Support;

    class AuditHostInformationBehavior : IBehavior<IAuditContext, IAuditContext>
    {
        public AuditHostInformationBehavior(HostInformation hostInfo, string endpoint)
        {
            this.hostInfo = hostInfo;
            this.endpoint = endpoint;
        }

        public Task Invoke(IAuditContext context, Func<IAuditContext, CancellationToken, Task> next, CancellationToken token)
        {
            context.AddAuditData(Headers.HostId, hostInfo.HostId.ToString("N"));
            context.AddAuditData(Headers.HostDisplayName, hostInfo.DisplayName);

            context.AddAuditData(Headers.ProcessingMachine, RuntimeEnvironment.MachineName);
            context.AddAuditData(Headers.ProcessingEndpoint, endpoint);

            return next(context, token);
        }

        readonly string endpoint;
        readonly HostInformation hostInfo;
    }
}