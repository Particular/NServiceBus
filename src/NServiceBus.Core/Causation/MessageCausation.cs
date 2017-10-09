namespace NServiceBus.Features
{
    using System;

    class MessageCausation : Feature
    {
        public MessageCausation()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var newIdGenerator = GetIdStrategy(context);

            context.Pipeline.Register("AttachCausationHeaders", new AttachCausationHeadersBehavior(newIdGenerator), "Adds related to and conversation id headers to outgoing messages");
        }

        static Func<ConversationIdStrategyContext, ConversationId> GetIdStrategy(FeatureConfigurationContext context)
        {
            if (context.Settings.TryGet("CustomConversationIdStrategy", out Func<ConversationIdStrategyContext, ConversationId> idGenerator))
            {
                return strategyContext =>
                {
                    try
                    {
                        return idGenerator(strategyContext);
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