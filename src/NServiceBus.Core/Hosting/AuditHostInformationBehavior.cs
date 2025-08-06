#nullable enable
namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Hosting;
using Pipeline;
using Support;

class AuditHostInformationBehavior(HostInformation hostInfo, string endpoint) : IBehavior<IAuditContext, IAuditContext>
{
    public Task Invoke(IAuditContext context, Func<IAuditContext, Task> next)
    {
        context.AuditMetadata[Headers.HostId] = hostInfo.HostId.ToString("N");
        context.AuditMetadata[Headers.HostDisplayName] = hostInfo.DisplayName;

        context.AuditMetadata[Headers.ProcessingMachine] = RuntimeEnvironment.MachineName;
        context.AuditMetadata[Headers.ProcessingEndpoint] = endpoint;

        return next(context);
    }
}