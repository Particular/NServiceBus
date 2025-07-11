#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Extensibility;
using Pipeline;
using Transport;

class AttachCausationHeadersBehavior(Func<IOutgoingLogicalMessageContext, string> conversationIdStrategy)
    : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
{
    public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
    {
        context.TryGetIncomingPhysicalMessage(out var incomingMessage);

        SetRelatedToHeader(context, incomingMessage);
        SetConversationIdHeader(context, incomingMessage);

        return next(context);
    }

    static void SetRelatedToHeader(IOutgoingLogicalMessageContext context, IncomingMessage? incomingMessage)
    {
        if (incomingMessage == null)
        {
            return;
        }

        context.Headers[Headers.RelatedTo] = incomingMessage.MessageId;
    }

    void SetConversationIdHeader(IOutgoingLogicalMessageContext context, IncomingMessage? incomingMessage)
    {
        var conversationIdFromCurrentMessageContext = default(string);
        var hasIncomingMessageConversationId = incomingMessage != null && incomingMessage.Headers.TryGetValue(Headers.ConversationId, out conversationIdFromCurrentMessageContext);
        var hasUserDefinedConversationId = context.Headers.TryGetValue(Headers.ConversationId, out var userDefinedConversationId);

        if (context.GetOperationProperties().TryGet<string>(NewConversationId, out var newConversationId))
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

    public const string NewConversationId = "NewConversationId";
}