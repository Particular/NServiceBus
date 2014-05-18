namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;

    public class JsonSerialization : Feature<Categories.Serializers>
    {
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance)
                 .ConfigureProperty(s => s.SkipArrayWrappingForSingleMessages, !config.Settings.GetOrDefault<bool>("SerializationSettings.WrapSingleMessages"));
        }
    }
}