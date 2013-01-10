namespace NServiceBus
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;

    public static class ConfigureJsonSerializer
    {
        public static Configure JsonSerializer(this Configure config, bool dontWrapSingleMessages = false)
        {
            config.Configurer.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance)
                  .ConfigureProperty(s => s.SkipArrayWrappingForSingleMessages, dontWrapSingleMessages);
            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);

            return config;
        }

        public static Configure BsonSerializer(this Configure config)
        {
            config.Configurer.ConfigureComponent<BsonMessageSerializer>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}