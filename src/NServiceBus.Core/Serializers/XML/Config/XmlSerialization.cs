﻿namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Serializers.XML;

    class XmlSerialization : Feature
    {
        internal XmlSerialization()
        {
            EnableByDefault();
            Prerequisite(this.ShouldSerializationFeatureBeEnabled, "XmlSerialization not enable since serialization definition not detected.");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            var c = context.Container.ConfigureComponent<XmlMessageSerializer>(DependencyLifecycle.SingleInstance);

            context.Settings.ApplyTo<XmlMessageSerializer>((IComponentConfig)c);
        }
    }
}