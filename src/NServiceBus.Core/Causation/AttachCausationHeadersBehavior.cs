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
            ApplyHeaders(context);

            return next(context);
        }

        static void ApplyHeaders(IOutgoingPhysicalMessageContext context)
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