namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Serializers.XML;

    public class XmlSerialization : Feature<Categories.Serializers>
    {
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            var c =  config.Configurer.ConfigureComponent<XmlMessageSerializer>(DependencyLifecycle.SingleInstance);

            config.Settings.ApplyTo<XmlMessageSerializer>((IComponentConfig)c);
        }
    }
}