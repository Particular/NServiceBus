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

        static Func<IOutgoingLogicalMessageContext, ConversationId> GetIdStrategy(ReadOnlySettings settings)
        {
            if (settings.TryGet("CustomConversationIdStrategy", out Func<ConversationIdStrategyContext, ConversationId> idGenerator))
            {
                return context =>
                {
                    try
                    {
                        return idGenerator(new ConversationIdStrategyContext(context.Message));
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"Failed to execute CustomConversationIdStrategy. This configuration option was defined using {nameof(EndpointConfiguration)}.{nameof(MessageCausationConfigurationExtensions.CustomConversationIdStrategy)}.", exception);
                    }
                };
            }

            return _ => ConversationId.Default;
        }
    }
}