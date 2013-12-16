namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Messages;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CreatePhysicalMessageBehavior : IBehavior<SendLogicalMessagesContext>
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void Invoke(SendLogicalMessagesContext context, Action next)
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
            foreach(var kvp in context.LogicalMessages.SelectMany(m=>m.Headers))
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }
           
            var messageDefinitions = context.LogicalMessages.Select(m => MessageMetadataRegistry.GetMessageDefinition(m.MessageType)).ToList();

            toSend.TimeToBeReceived = messageDefinitions.Min(md => md.TimeToBeReceived);
            toSend.Recoverable = messageDefinitions.Any(md => md.Recoverable);

            context.Set(toSend);

            PipelineExecutor.InvokeSendPipeline(sendOptions,toSend);

            next();
        }
    }
}