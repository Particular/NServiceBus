namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Serializers.XML;

    /// <summary>
    /// Used to configure xml as a message serializer
    /// </summary>
    public class XmlSerialization : Feature
    {
        
        internal XmlSerialization()
        {
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            var c = context.Container.ConfigureComponent<XmlMessageSerializer>(DependencyLifecycle.SingleInstance);

            context.Settings.ApplyTo<XmlMessageSerializer>((IComponentConfig)c);
        }
    }
}