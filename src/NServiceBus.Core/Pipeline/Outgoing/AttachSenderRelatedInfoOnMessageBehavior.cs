namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class AttachSenderRelatedInfoOnMessageBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            var message = context.Message;

            if (!message.Headers.ContainsKey(Headers.NServiceBusVersion))
            {
                message.Headers[Headers.NServiceBusVersion] = GitVersionInformation.MajorMinorPatch;
            }

            if (!message.Headers.ContainsKey(Headers.TimeSent))
            {
                message.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            }
            return next(context, cancellationToken);
        }
    }
}