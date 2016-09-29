namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Hosting;
    using Pipeline;
    using Support;

    class AddHostInfoHeadersBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public AddHostInfoHeadersBehavior(HostInformation hostInformation, string endpoint)
        {
            this.hostInformation = hostInformation;
            this.endpoint = endpoint;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            context.Headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
            context.Headers[Headers.OriginatingEndpoint] = endpoint;
            context.Headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");

            return next(context);
        }

        string endpoint;
        HostInformation hostInformation;
    }
}