namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Pipeline.Contexts;
    using TransportDispatch;
    using Transports;

    class AttachCorrelationIdBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            var correlationId = context.GetOrCreate<State>().CustomCorrelationId;

            //if we don't have a explicit correlation id set
            if (string.IsNullOrEmpty(correlationId))
            {
                IncomingMessage current;

                //try to get it from the incoming message
                if (context.TryGetIncomingPhysicalMessage(out current))
                {
                    string incomingCorrelationId;

                    if (current.Headers.TryGetValue(Headers.CorrelationId, out incomingCorrelationId))
                    {
                        correlationId = incomingCorrelationId;
                    }
                }
            }

            //if we still doesn't have one we'll use the message id
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = context.GetMessageId();
            }

            context.SetHeader(Headers.CorrelationId, correlationId);
            return next();
        }

        public class State
        {
            public string CustomCorrelationId { get; set; }
        }
    }
}