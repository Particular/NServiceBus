namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Outgoing;
    using NServiceBus.Transports;
    using OutgoingPipeline;
    using Pipeline;

    class AttachCausationHeadersBehavior : Behavior<OutgoingPhysicalMessageContext>
    {
        public override Task Invoke(OutgoingPhysicalMessageContext context, Func<Task> next)
        {
            ApplyHeaders(context);

            return next();
        }

        void ApplyHeaders(OutgoingPhysicalMessageContext context)
        {
            var conversationId = CombGuid.Generate().ToString();

            IncomingMessage incomingMessage;

            if (context.TryGetIncomingPhysicalMessage(out incomingMessage))
            {
                context.Headers[Headers.RelatedTo] = incomingMessage.MessageId;

                string conversationIdFromCurrentMessageContext;
                if (incomingMessage.Headers.TryGetValue(Headers.ConversationId, out conversationIdFromCurrentMessageContext))
                {
                    conversationId = conversationIdFromCurrentMessageContext;
                }
            }

            context.Headers[Headers.ConversationId] = conversationId;
        }
    }
}