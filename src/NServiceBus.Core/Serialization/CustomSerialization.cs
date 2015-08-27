namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Serialization;

    class CustomSerialization : ConfigureSerialization
    {
        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected override Type GetSerializerType(FeatureConfigurationContext context)
        {
            return context.Settings.Get<Type>("CustomSerializerType");
        }
    }
}