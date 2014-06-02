namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;

    /// <summary>
    /// Json Serialization
    /// </summary>
    public class JsonSerialization : Feature
    {
        
        internal JsonSerialization()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance)
                 .ConfigureProperty(s => s.SkipArrayWrappingForSingleMessages, !context.Settings.GetOrDefault<bool>("SerializationSettings.WrapSingleMessages"));
        }
    }
}