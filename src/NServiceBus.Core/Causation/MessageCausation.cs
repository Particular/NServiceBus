namespace NServiceBus.Features
{
    using System;
    using Pipeline;
    using Settings;

    class MessageCausation : Feature
    {
        public MessageCausation()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var newIdGenerator = GetIdStrategy(context.Settings);

            context.Pipeline.Register("AttachCausationHeaders", new AttachCausationHeadersBehavior(newIdGenerator), "Adds related to and conversation id headers to outgoing messages");
        }

        static Func<IOutgoingLogicalMessageContext, string> GetIdStrategy(IReadOnlySettings settings)
        {
            if (settings.TryGet("CustomConversationIdStrategy", out Func<ConversationIdStrategyContext, ConversationId> idGenerator))
            {
                return WrapUserDefinedInvocation(idGenerator);
            }

            return _ => CombGuid.Generate().ToString();
        }

        internal static Func<IOutgoingLogicalMessageContext, string> WrapUserDefinedInvocation(Func<ConversationIdStrategyContext, ConversationId> userDefinedIdGenerator)
        {
            return context =>
            {
                ConversationId customConversationId;

                try
                {
                    customConversationId = userDefinedIdGenerator(new ConversationIdStrategyContext(context.Message, context.Headers));
                }
                catch (Exception exception)
                {
                    throw new Exception($"Failed to execute the custom conversation ID strategy defined using '{nameof(EndpointConfiguration)}.{nameof(MessageCausationConfigurationExtensions.CustomConversationIdStrategy)}'.", exception);
                }

                if (customConversationId == null)
                {
                    throw new Exception($"The custom conversation ID strategy defined using '{nameof(EndpointConfiguration)}.{nameof(MessageCausationConfigurationExtensions.CustomConversationIdStrategy)}' returned null. The custom strategy must return either '{nameof(ConversationId)}.{nameof(ConversationId.Custom)}(customValue)' or '{nameof(ConversationId)}.{nameof(ConversationId.Default)}'");
                }

                return customConversationId.Value;
            };
        }


    }
}