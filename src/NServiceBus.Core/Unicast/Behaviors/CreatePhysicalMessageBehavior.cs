namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Messages;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CreatePhysicalMessageBehavior : IBehavior<SendLogicalMessagesContext>
    {
        MessageMetadataRegistry messageMetadataRegistry;
        UnicastBus unicastBus;
        PipelineFactory pipelineFactory;

        internal CreatePhysicalMessageBehavior(PipelineFactory pipelineFactory, MessageMetadataRegistry messageMetadataRegistry, UnicastBus unicastBus)
        {
            this.pipelineFactory = pipelineFactory;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.unicastBus = unicastBus;
        }

        internal Address DefaultReplyToAddress { get; set; }

        public void Invoke(SendLogicalMessagesContext context, Action next)
        {
            var sendOptions = context.SendOptions;

            var toSend = new TransportMessage
            {
                MessageIntent = sendOptions.Intent,
                CorrelationId = sendOptions.CorrelationId,
                ReplyToAddress = sendOptions.ReplyToAddress
            };

            //apply static headers
            foreach (var kvp in unicastBus.OutgoingHeaders)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            //apply individual headers
            foreach(var kvp in context.LogicalMessages.SelectMany(m=>m.Headers))
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }
                
            if (toSend.ReplyToAddress == null)
            {
                toSend.ReplyToAddress = DefaultReplyToAddress;
            }

            //todo: pull this out to the distributor when we split it to a separate repo
            if (unicastBus.PropagateReturnAddressOnSend)
            {
                var incomingMessage = context.IncomingMessage;

                if (incomingMessage != null)
                {
                    sendOptions.ReplyToAddress = incomingMessage.ReplyToAddress;
                }
            }


            var messageDefinitions = context.LogicalMessages.Select(m => messageMetadataRegistry.GetMessageDefinition(m.MessageType)).ToList();

            toSend.TimeToBeReceived = messageDefinitions.Min(md => md.TimeToBeReceived);
            toSend.Recoverable = messageDefinitions.Any(md => md.Recoverable);

            context.Set(toSend);

            pipelineFactory.InvokeSendPipeline(sendOptions,toSend);

            next();
        }
    }
}