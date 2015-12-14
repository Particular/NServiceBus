namespace NServiceBus.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Features;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;

    /// <summary>
    /// Base class for configuring <see cref="SerializationDefinition"/> features.
    /// </summary>
    public abstract class ConfigureSerialization : Feature
    {
        /// <inheritdoc />
        protected ConfigureSerialization()
        {
            EnableByDefault();
            Prerequisite(context => IsDefaultSerializer(context) || IsAdditionalDeserializer(context),
                $@"{GetType()} not enabled since serialization definition not detected.");
        }

        /// <summary>
        /// Called when the features is activated.
        /// </summary>
        protected internal sealed override void Setup(FeatureConfigurationContext context)
        {
            var serializerType = GetSerializerType(context);
            if (serializerType == null)
            {
                return;
            }

            RegisterSerializer(context, serializerType);

            if (IsDefaultSerializer(context))
            {
                context.Container.ConfigureComponent(b => new MessageDeserializerResolver(b.BuildAll<IMessageSerializer>(), serializerType), DependencyLifecycle.SingleInstance);
            }
            context.Container.ConfigureComponent(builder =>
            {
                var mapper = new MessageMapper();
                var settings = context.Settings;
                var conventions = settings.Get<Conventions>();
                var messageTypes = settings.GetAvailableTypes().Where(conventions.IsMessageType);
                mapper.Initialize(messageTypes);
                return mapper;
            },  DependencyLifecycle.SingleInstance);
        }

        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected abstract Type GetSerializerType(FeatureConfigurationContext context);

        /// <summary>
        /// Registeres the specified implementation of <see cref="IMessageSerializer"/>.
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

        bool IsDefaultSerializer(FeatureConfigurationContext context)
        {
            Guard.AgainstNull(nameof(context), context);

            var serializationDefinition = context.Settings.GetSelectedSerializer();
            return serializationDefinition.ProvidedByFeature() == GetType();
        }

        bool IsAdditionalDeserializer(FeatureConfigurationContext context)
        {
            Guard.AgainstNull(nameof(context), context);

            Dictionary<RuntimeTypeHandle, SerializationDefinition> deserializers;
            if (!context.Settings.TryGet("AdditionalDeserializers", out deserializers))
            {
                return false;
            }

            return deserializers.ContainsKey(GetType().TypeHandle);
        }

    }
}