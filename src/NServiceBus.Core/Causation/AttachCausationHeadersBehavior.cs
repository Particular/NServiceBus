namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class AttachCausationHeadersBehavior : IBehavior<IOutgoingPhysicalMessageContext, IOutgoingPhysicalMessageContext>
    {
        public Task Invoke(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> next)
        {
            IncomingMessage incomingMessage;
            context.TryGetIncomingPhysicalMessage(out incomingMessage);

            SetRelatedToHeader(context, incomingMessage);
            SetConversationIdHeader(context, incomingMessage);

            return next(context);
        }

        static void SetRelatedToHeader(IOutgoingPhysicalMessageContext context, IncomingMessage incomingMessage)
        {
            if (incomingMessage == null)
            {
                return;
            }

            context.Headers[Headers.RelatedTo] = incomingMessage.MessageId;
        }

        static void SetConversationIdHeader(IOutgoingPhysicalMessageContext context, IncomingMessage incomingMessage)
        {
            string conversationIdFromCurrentMessageContext;
            string userDefinedConversationId;
            var hasUserDefinedConversationId = context.Headers.TryGetValue(Headers.ConversationId, out userDefinedConversationId);

            if (incomingMessage != null && incomingMessage.Headers.TryGetValue(Headers.ConversationId, out conversationIdFromCurrentMessageContext))
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
                // do not override user defined conversation id if no incoming message exists.
                return;
            }

            context.Headers[Headers.ConversationId] = CombGuid.Generate().ToString();
        }
    }
}