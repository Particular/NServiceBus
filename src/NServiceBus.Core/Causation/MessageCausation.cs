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
            var newIdGenerator = GetIdGenerator(context);

            context.Pipeline.Register("AttachCausationHeaders", new AttachCausationHeadersBehavior(newIdGenerator), "Adds related to and conversation id headers to outgoing messages");
        }

        static Func<ConversationIdGeneratorContext, string> GetIdGenerator(FeatureConfigurationContext context)
        {
            var settings = context.Settings;
            if (settings.TryGet<Func<ConversationIdGeneratorContext, string>>(MessageCausationConfigurationExtensions.CustomConversationIdGeneratorKey, out var idGenerator))
            {
                return generatorContext =>
                {
                    try
                    {
                        return idGenerator(generatorContext);
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"Failed to execute CustomConversationIdGenerator. This configuration option was defined using {nameof(EndpointConfiguration)}.{nameof(MessageCausationConfigurationExtensions.CustomConversationIdGenerator)}.", exception);
                    }
                };
            }
            return _ => CombGuid.Generate().ToString();
        }
    }
}