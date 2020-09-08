namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class InferredMessageTypeEnricherBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next, CancellationToken cancellationToken)
        {
            if (!context.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                context.Headers[Headers.EnclosedMessageTypes] = context.Message.MessageType.FullName;
            }

            return next();
        }
    }
}