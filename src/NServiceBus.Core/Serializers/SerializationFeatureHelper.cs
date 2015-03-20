namespace NServiceBus.Features
{
    /// <summary>
    /// Base class for all serialization <see cref="Feature"/>s.
    /// </summary>
    public static class SerializationFeatureHelper
    {
        /// <summary>
        /// Allows serialization features to verify their <see cref="Feature"/> Prerequisites
        /// </summary>
        public static bool ShouldSerializationFeatureBeEnabled(this Feature serializationFeature, FeatureConfigurationContext context)
        {
            Guard.AgainstNull(serializationFeature, "serializationFeature");
            Guard.AgainstNull(context, "context");
            var serializationDefinition = context.Settings.GetSelectedSerializer();
            return serializationDefinition.ProvidedByFeature() == serializationFeature.GetType();
        }
    }
}