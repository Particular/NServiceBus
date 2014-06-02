namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Serializers.XML;


    /// <summary>
    /// Used to control xml as a message serializer
    /// </summary>
    public class XmlSerialization : Feature
    {
        
        internal XmlSerialization()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            var c = context.Container.ConfigureComponent<XmlMessageSerializer>(DependencyLifecycle.SingleInstance);

            context.Settings.ApplyTo<XmlMessageSerializer>((IComponentConfig)c);
        }
    }
}