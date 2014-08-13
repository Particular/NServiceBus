﻿namespace NServiceBus.Features
{

    /// <summary>
    /// Base class for all serialization <see cref="Feature"/>s.
    /// </summary>
    public static class SerializationFeatureHelper 
    {
        /// <summary>
        /// Allows serialization features to verify their <see cref="Feature.Prerequisite"/>s
        /// </summary>
        public static bool ShouldSerializationFeatureBeEnabled(this Feature serializationFeature, FeatureConfigurationContext context)
        {
            var serializationDefinition = context.Settings.GetSelectedSerializer();
            return serializationDefinition.ProvidedByFeature() == serializationFeature.GetType();
        }
    }
}