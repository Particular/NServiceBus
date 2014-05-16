namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;
    using Settings;

    public class JsonSerialization : Feature<Categories.Serializers>
    {
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance)
                 .ConfigureProperty(s => s.SkipArrayWrappingForSingleMessages, !SettingsHolder.GetOrDefault<bool>("SerializationSettings.WrapSingleMessages"));
        }
    }
}