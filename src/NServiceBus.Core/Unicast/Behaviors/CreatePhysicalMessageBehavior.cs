namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Messages;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CreatePhysicalMessageBehavior : IBehavior<SendLogicalMessageContext>
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            var sendOptions = context.SendOptions;

            var toSend = new TransportMessage
            {
                MessageIntent = sendOptions.Intent,
                ReplyToAddress = sendOptions.ReplyToAddress
            };

            if (sendOptions.CorrelationId != null)
            {
                toSend.CorrelationId = sendOptions.CorrelationId;
            }

            //apply static headers
            foreach (var kvp in UnicastBus.OutgoingHeaders)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            //apply individual headers
            foreach (var kvp in context.MessageToSend.Headers)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            var messageDefinitions = MessageMetadataRegistry.GetMessageDefinition(context.MessageToSend.MessageType);

            toSend.TimeToBeReceived = messageDefinitions.TimeToBeReceived;
            toSend.Recoverable = messageDefinitions.Recoverable;

            context.Set(toSend);

            PipelineExecutor.InvokeSendPipeline(sendOptions,toSend);

            next();
        }
    }
}