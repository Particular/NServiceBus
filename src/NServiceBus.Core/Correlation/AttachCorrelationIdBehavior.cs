namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class AttachCorrelationIdBehavior : Behavior<OutgoingContext>
    {
        public override Task Invoke(OutgoingContext context, Func<Task> next)
        {
            var correlationId = context.GetOrCreate<State>().CustomCorrelationId;
       
            //if we don't have a explicit correlation id set
            if (string.IsNullOrEmpty(correlationId))
            {
                TransportMessage current;

                //try to get it from the incoming message
                if (context.TryGetIncomingPhysicalMessage(out current))
                {
                    string incomingCorrelationId;
                        
                    if (current.Headers.TryGetValue(Headers.CorrelationId,out incomingCorrelationId))
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

        public class Registration : RegisterStep
        {
            public Registration()
                : base("AttachCorrelationId", typeof(AttachCorrelationIdBehavior), "Makes sure that outgoing messages have a correlation id header set")
            {
            }
        }
    }
}