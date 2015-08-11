namespace NServiceBus.Serialization
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serializers;

    /// <summary>
    /// Base class for configuring <see cref="SerializationDefinition"/> features.
    /// </summary>
    public abstract class ConfigureSerialization : Feature
    {
        /// <inheritdoc />
        protected ConfigureSerialization()
        {
            EnableByDefault();
            Prerequisite(context => this.IsDefaultSerializer(context) || this.IsAdditionalDeserializer(context), 
                string.Format("{0} not enabled since serialization definition not detected.", GetType()));
        }

        /// <summary>
        /// Called when the features is activated.
        /// </summary>
        protected internal sealed override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageDeserializerResolver>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);

            var serializerType = GetSerializerType(context);
            if (serializerType == null)
            {
                return;
            }

            RegisterSerializer(context, serializerType);

            if (this.IsDefaultSerializer(context))
            {
                context.Container.ConfigureProperty<MessageDeserializerResolver>(resolver => resolver.DefaultSerializerType, serializerType);
            }
        }

        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected abstract Type GetSerializerType(FeatureConfigurationContext context);

        /// <summary>
        /// Registeres the specified implementation of <see cref="IMessageSerializer"/>
        /// </summary>
        protected virtual void RegisterSerializer(FeatureConfigurationContext context, Type serializerType)
        {
            if (!typeof(IMessageSerializer).IsAssignableFrom(serializerType))
            {
                throw new InvalidOperationException("The type needs to implement IMessageSerializer.");
            }

            var c = context.Container.ConfigureComponent(serializerType, DependencyLifecycle.SingleInstance);
            context.Settings.ApplyTo(serializerType, c);
        }
    }
}