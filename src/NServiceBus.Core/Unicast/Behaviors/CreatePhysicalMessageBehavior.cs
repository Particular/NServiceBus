namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Transport;
    using Unicast;
    using Messages;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CreatePhysicalMessageBehavior : IBehavior<OutgoingContext>
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            var sendOptions = context.SendOptions;
            TransportMessage toSend;
            var isControl = false;

            if (context.OutgoingLogicalMessage.IsControlMessage())
            {
                toSend = ControlMessage.Create(Address.Local);
                toSend.MessageIntent = sendOptions.Intent;
                isControl = true;
            }
            else
            {
                toSend = new TransportMessage
                {
                    MessageIntent = sendOptions.Intent,
                    ReplyToAddress = sendOptions.ReplyToAddress
                };
            }

            if (sendOptions.CorrelationId != null)
            {
                toSend.CorrelationId = sendOptions.CorrelationId;
            }

            if (!isControl)
            {
                //apply static headers
                foreach (var kvp in UnicastBus.OutgoingHeaders)
                {
                    toSend.Headers[kvp.Key] = kvp.Value;
                }
            }
            //apply individual headers
            foreach (var kvp in context.OutgoingLogicalMessage.Headers)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            if (!isControl)
            {
                var messageDefinitions = MessageMetadataRegistry.GetMessageDefinition(context.OutgoingLogicalMessage.MessageType);

                toSend.TimeToBeReceived = messageDefinitions.TimeToBeReceived;
                toSend.Recoverable = messageDefinitions.Recoverable;
            }

            context.Set(toSend);

            next();
        }
    }
}