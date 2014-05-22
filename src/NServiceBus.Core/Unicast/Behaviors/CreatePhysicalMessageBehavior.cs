namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Messages;

    class CreatePhysicalMessageBehavior : IBehavior<OutgoingContext>
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            var deliveryOptions = context.DeliveryOptions;

            var toSend = new TransportMessage
            {
                MessageIntent = MessageIntentEnum.Publish,
                ReplyToAddress = deliveryOptions.ReplyToAddress
            };

            var sendOptions = deliveryOptions as SendOptions;


            if (sendOptions != null && sendOptions.CorrelationId != null)
            {
                toSend.MessageIntent = sendOptions is ReplyOptions ? MessageIntentEnum.Reply : MessageIntentEnum.Send;
                toSend.CorrelationId = sendOptions.CorrelationId;
            }

            //apply static headers
            foreach (var kvp in UnicastBus.OutgoingHeaders)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            //apply individual headers
            foreach (var kvp in context.OutgoingLogicalMessage.Headers)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            var messageDefinitions = MessageMetadataRegistry.GetMessageDefinition(context.OutgoingLogicalMessage.MessageType);

            toSend.TimeToBeReceived = messageDefinitions.TimeToBeReceived;
            toSend.Recoverable = messageDefinitions.Recoverable;

            context.Set(toSend);

            next();
        }
    }
}