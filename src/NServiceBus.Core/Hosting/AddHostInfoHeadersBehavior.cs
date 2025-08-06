#nullable enable
namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Hosting;
using Pipeline;
using Support;

class AddHostInfoHeadersBehavior(HostInformation hostInformation, string endpoint)
    : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
{
    public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
    {
        context.Headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
        context.Headers[Headers.OriginatingEndpoint] = endpoint;
        context.Headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");

        return next(context);
    }
}