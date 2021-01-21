namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class AttachCausationHeadersBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public AttachCausationHeadersBehavior(Func<IOutgoingLogicalMessageContext, string> conversationIdStrategy)
        {
            this.conversationIdStrategy = conversationIdStrategy;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken token)
        {
            context.TryGetIncomingPhysicalMessage(out var incomingMessage);

            SetRelatedToHeader(context, incomingMessage);
            SetConversationIdHeader(context, incomingMessage);

            return next(context, token);
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
            var conversationIdFromCurrentMessageContext = default(string);
            var hasIncomingMessageConversationId = incomingMessage != null && incomingMessage.Headers.TryGetValue(Headers.ConversationId, out conversationIdFromCurrentMessageContext);
            var hasUserDefinedConversationId = context.Headers.TryGetValue(Headers.ConversationId, out var userDefinedConversationId);

            if (context.Extensions.TryGet<string>(NewConversationId, out var newConversationId))
            {
                if (hasUserDefinedConversationId)
                {
                    throw new Exception($"Cannot set the {Headers.ConversationId} header to '{userDefinedConversationId}' as StartNewConversation() was called.");
                }
                if (hasIncomingMessageConversationId)
                {
                    context.Headers[Headers.PreviousConversationId] = conversationIdFromCurrentMessageContext;
                }
                context.Headers[Headers.ConversationId] = newConversationId ?? conversationIdStrategy(context);
                return;
            }

            if (hasIncomingMessageConversationId)
            {
                if (hasUserDefinedConversationId)
                {
                    throw new Exception($"Cannot set the {Headers.ConversationId} header to '{userDefinedConversationId}' as it cannot override the incoming header value ('{conversationIdFromCurrentMessageContext}'). To start a new conversation use sendOptions.StartNewConversation().");
                }

                context.Headers[Headers.ConversationId] = conversationIdFromCurrentMessageContext;
                return;
            }

            if (hasUserDefinedConversationId)
            {
                return;
            }

            context.Headers[Headers.ConversationId] = conversationIdStrategy(context);
        }

        readonly Func<IOutgoingLogicalMessageContext, string> conversationIdStrategy;
        public const string NewConversationId = "NewConversationId";
    }
}