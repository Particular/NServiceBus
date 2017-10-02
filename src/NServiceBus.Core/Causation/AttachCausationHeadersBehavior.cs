namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class AttachCausationHeadersBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public AttachCausationHeadersBehavior(Func<ConversationIdGeneratorContext, string> idGenerator)
        {
            this.idGenerator = idGenerator;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            context.TryGetIncomingPhysicalMessage(out var incomingMessage);

            SetRelatedToHeader(context, incomingMessage);
            SetConversationIdHeader(context, incomingMessage);

            return next(context);
        }

        static void SetRelatedToHeader(IOutgoingLogicalMessageContext context, IncomingMessage incomingMessage)
        {
            if (incomingMessage == null)
            {
                return;
            }

            context.Headers[Headers.RelatedTo] = incomingMessage.MessageId;
        }

        void SetConversationIdHeader(IOutgoingLogicalMessageContext context, IncomingMessage incomingMessage)
        {
            var hasUserDefinedConversationId = context.Headers.TryGetValue(Headers.ConversationId, out var userDefinedConversationId);

            if (incomingMessage != null && incomingMessage.Headers.TryGetValue(Headers.ConversationId, out var conversationIdFromCurrentMessageContext))
            {
                if (hasUserDefinedConversationId)
                {
                    throw new Exception($"Cannot set the {Headers.ConversationId} header to '{userDefinedConversationId}' as it cannot override the incoming header value ('{conversationIdFromCurrentMessageContext}').");
                }

                context.Headers[Headers.ConversationId] = conversationIdFromCurrentMessageContext;
                return;
            }

            if (hasUserDefinedConversationId)
            {
                return;
            }

            context.Headers[Headers.ConversationId] = idGenerator(new ConversationIdGeneratorContext(context.Message));
        }

        Func<ConversationIdGeneratorContext, string> idGenerator;
    }
}