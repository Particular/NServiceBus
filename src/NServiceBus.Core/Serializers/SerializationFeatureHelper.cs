namespace NServiceBus.Features
{
    using NServiceBus.Serialization;
    using NServiceBus.Utils.Reflection;

    /// <summary>
    /// Base class for all serialization <see cref="Feature"/>s.
    /// </summary>
    public static class SerializationFeatureHelper 
    {
        /// <summary>
        /// Allows serialization features to verify their Prerequisites
        /// </summary>
        public static bool ShouldSerializationFeatureBeEnabled(this Feature serializationFeature, FeatureConfigurationContext context)
        {
            var selectedSerializerType = context.Settings.GetSelectedSerializerType();
            var serializationDefinition = selectedSerializerType.Construct<ISerializationDefinition>();
            return serializationDefinition.ProvidedByFeature == serializationFeature.GetType();
        }
    }
}