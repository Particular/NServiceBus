namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class AttachSenderRelatedInfoOnMessageBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            var message = context.Message;

            if (!message.Headers.ContainsKey(Headers.NServiceBusVersion))
            {
                message.Headers[Headers.NServiceBusVersion] = GitFlowVersion.MajorMinorPatch;
            }

            if (!message.Headers.ContainsKey(Headers.TimeSent))
            {
                message.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            }
            return next(context);
        }
    }
}