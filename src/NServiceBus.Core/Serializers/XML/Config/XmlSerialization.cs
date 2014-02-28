namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.XML;
    using Settings;

    public class XmlSerialization : Feature<Categories.Serializers>
    {
        public override void Initialize()
        {
            Configure.Component<MessageMapper>(DependencyLifecycle.SingleInstance);
            Configure.Component<XmlMessageSerializer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SkipWrappingElementForSingleMessages, !SettingsHolder.GetOrDefault<bool>("SerializationSettings.WrapSingleMessages"));

            SettingsHolder.ApplyTo<XmlMessageSerializer>();
        }
    }
}