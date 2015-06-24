namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class OutboxDispatchStrategy : DispatchStrategy
    {
        OutboxMessage currentOutboxMessage;
      
        public OutboxDispatchStrategy(OutboxMessage currentOutboxMessage)
        {
            this.currentOutboxMessage = currentOutboxMessage;
        }

        public override void Dispatch(IDispatchMessages dispatcher,OutgoingMessage message,
            RoutingStrategy routingStrategy,
            ConsistencyGuarantee minimumConsistencyGuarantee,
            IEnumerable<DeliveryConstraint> constraints,
            BehaviorContext currentContext)
        {
          
            var options = new Dictionary<string, string>();

            constraints.ToList().ForEach(c => c.Serialize(options));

            routingStrategy.Serialize(options);
          
            currentOutboxMessage.TransportOperations.Add(new TransportOperation(message.MessageId, options, message.Body, message.Headers));                    
        }
    }
}