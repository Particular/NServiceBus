namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.XML;
    using Settings;

    public class XmlSerialization : Feature<Categories.Serializers>
    {
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<XmlMessageSerializer>(DependencyLifecycle.SingleInstance);

            SettingsHolder.ApplyTo<XmlMessageSerializer>();
        }
    }
}