namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Mutator to set the related to header
    /// </summary>
    class AttachCausationHeadersBehavior :PhysicalOutgoingContextStageBehavior
    {

        public override void Invoke(Context context, Action next)
        {
            ApplyHeaders(context);

            next();
         }

        void ApplyHeaders(Context context)
        {
            var transportMessage = context.OutgoingMessage;
 
            if (transportMessage.Headers.ContainsKey(Headers.ConversationId))
                return;

            var conversationId = CombGuid.Generate().ToString();

            TransportMessage incomingMessage;

            if (context.TryGet(TransportReceiveContext.IncomingPhysicalMessageKey,out incomingMessage))
            {
                transportMessage.Headers[Headers.RelatedTo] = incomingMessage.Id;

                string conversationIdFromCurrentMessageContext;
                if (incomingMessage.Headers.TryGetValue(Headers.ConversationId, out conversationIdFromCurrentMessageContext))
                {
                    conversationId = conversationIdFromCurrentMessageContext;
                }
            }

            transportMessage.Headers[Headers.ConversationId] = conversationId;
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("AttachCausationHeaders", typeof(AttachCausationHeadersBehavior), "Adds related to and conversation id headers to outgoing messages")
            {
            }
        }
    }
}