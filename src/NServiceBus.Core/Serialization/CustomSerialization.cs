namespace NServiceBus.Features
{
    using System;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;

    class CustomSerialization : Feature
    {
        public CustomSerialization()
        {
            EnableByDefault();
            Prerequisite(this.ShouldSerializationFeatureBeEnabled, "CustomSerialization not enable since serialization definition not detected.");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(context.Settings.Get<Type>("CustomSerializerType"), DependencyLifecycle.SingleInstance);
        }
    }
}