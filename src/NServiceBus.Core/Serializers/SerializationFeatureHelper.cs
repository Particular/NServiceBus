namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;

    /// <summary>
    /// Base class for all serialization <see cref="Feature"/>s.
    /// </summary>
    public static class SerializationFeatureHelper
    {
        /// <summary>
        /// Allows serialization features to verify their <see cref="ConfigureSerialization"/> Prerequisites.
        /// </summary>
        public static bool IsDefaultSerializer(this ConfigureSerialization serializationFeature, FeatureConfigurationContext context)
        {
            Guard.AgainstNull("serializationFeature", serializationFeature);
            Guard.AgainstNull("context", context);

            var serializationDefinition = context.Settings.GetSelectedSerializer();
            return serializationDefinition.ProvidedByFeature() == serializationFeature.GetType();
        }

        /// <summary>
        /// Allows serialization features to check whether a given <see cref="ConfigureSerialization">feature</see> is configured as an additional deserializer.
        /// </summary>
        public static bool IsAdditionalDeserializer(this ConfigureSerialization serializationFeature, FeatureConfigurationContext context)
        {
            Guard.AgainstNull("serializationFeature", serializationFeature);
            Guard.AgainstNull("context", context);

            HashSet<SerializationDefinition> deserializers;
            if (!context.Settings.TryGet("AdditionalDeserializers", out deserializers))
            {
                return false;
            }

            return deserializers.Any(definition => definition.ProvidedByFeature() == serializationFeature.GetType());
        }
    }
}