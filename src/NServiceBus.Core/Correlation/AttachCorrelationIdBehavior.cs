namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;

    class AttachCorrelationIdBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            var correlationId = context.Extensions.GetOrCreateScoped<State>(context.MessageId).CustomCorrelationId;

            //if we don't have a explicit correlation id set
            if (string.IsNullOrEmpty(correlationId))
            {
                //try to get it from the incoming message
                if (context.TryGetIncomingPhysicalMessage(out var current))
                {
                    if (current.Headers.TryGetValue(Headers.CorrelationId, out var incomingCorrelationId))
                    {
                        correlationId = incomingCorrelationId;
                    }

                    if (string.IsNullOrEmpty(correlationId) && current.Headers.TryGetValue(Headers.MessageId, out incomingCorrelationId))
                    {
                        correlationId = incomingCorrelationId;
                    }
                }
            }

            //if we still doesn't have one we'll use the message id
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = context.MessageId;
            }

            context.Headers[Headers.CorrelationId] = correlationId;
            return next(context);
        }

        public class State
        {
            public string CustomCorrelationId { get; set; }
        }
    }
}