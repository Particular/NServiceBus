namespace NServiceBus
{
    using System;
    using NServiceBus.Unicast.Messages;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class CreatePhysicalMessageBehavior : IBehavior<OutgoingContext>
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void Invoke(OutgoingContext context, Action next)
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
            foreach (var kvp in UnicastBus.OutgoingHeaders)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            //apply individual headers
            foreach (var kvp in context.OutgoingLogicalMessage.Headers)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            if (context.OutgoingLogicalMessage.MessageType != null)
            {
                var messageDefinitions = MessageMetadataRegistry.GetMessageMetadata(context.OutgoingLogicalMessage.MessageType);

                toSend.TimeToBeReceived = messageDefinitions.TimeToBeReceived;
                toSend.Recoverable = messageDefinitions.Recoverable;
            }

            context.Set(toSend);

            next();
        }
    }
}