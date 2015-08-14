namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

    class AttachCausationHeadersBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override Task Invoke(Context context, Func<Task> next)
        {
            ApplyHeaders(context);

            return next();
         }

        void ApplyHeaders(Context context)
        {
            var conversationId = CombGuid.Generate().ToString();

            TransportMessage incomingMessage;

            if (context.TryGetIncomingPhysicalMessage(out incomingMessage))
            {
                context.SetHeader(Headers.RelatedTo,incomingMessage.Id);

                string conversationIdFromCurrentMessageContext;
                if (incomingMessage.Headers.TryGetValue(Headers.ConversationId, out conversationIdFromCurrentMessageContext))
                {
                    conversationId = conversationIdFromCurrentMessageContext;
                }
            }

            context.SetHeader(Headers.ConversationId,conversationId);
        }
    }
}