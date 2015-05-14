namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    class AttachCorrelationIdBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            var correlationId = context.Extensions.GetOrCreate<State>().CustomCorrelationId;
       
            //if we don't have a explicit correlation id set
            if (string.IsNullOrEmpty(correlationId))
            {
                TransportMessage current;

                //try to get it from the incoming message
                if (context.TryGet(TransportReceiveContext.IncomingPhysicalMessageKey, out current))
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
            next();
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