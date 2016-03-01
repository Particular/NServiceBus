namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class AttachSenderRelatedInfoOnMessageBehavior : Behavior<IRoutingContext>
    {
        public override Task Invoke(IRoutingContext context, Func<Task> next)
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
            return next();
        }
    }
}