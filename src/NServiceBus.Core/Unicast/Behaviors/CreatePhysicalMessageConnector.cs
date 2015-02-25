namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;

    class CreatePhysicalMessageConnector : StageConnector<OutgoingContext, PhysicalOutgoingContextStageBehavior.Context>
    {
        readonly IBus unicastBus;

        public CreatePhysicalMessageConnector(IBus unicastBus)
        {
            this.unicastBus = unicastBus;
        }

        public override void Invoke(OutgoingContext context, Action<PhysicalOutgoingContextStageBehavior.Context> next)
        {
            var deliveryOptions = context.DeliveryOptions;

            var toSend = new TransportMessage { MessageIntent = MessageIntentEnum.Publish };

            var sendOptions = deliveryOptions as SendOptions;


            if (sendOptions != null)
            {
                toSend.MessageIntent = sendOptions is ReplyOptions ? MessageIntentEnum.Reply : MessageIntentEnum.Send;

                if (sendOptions.CorrelationId != null)
                {
                    toSend.CorrelationId = sendOptions.CorrelationId;
                }
            }

            //apply static headers
            foreach (var kvp in unicastBus.OutgoingHeaders)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            //apply individual headers
            foreach (var kvp in context.OutgoingLogicalMessage.Headers)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            next(new PhysicalOutgoingContextStageBehavior.Context(toSend,context));
        }

    }
}