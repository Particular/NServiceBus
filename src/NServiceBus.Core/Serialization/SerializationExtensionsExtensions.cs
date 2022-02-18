namespace NServiceBus
{
    using Serialization;
    using Settings;

    /// <summary>
    /// Provides extensions methods for the <see cref="SerializationExtensions{T}"/> class.
    /// </summary>
    public static class SerializationExtensionsExtensions
    {
        /// <summary>
        /// Disables inference of message type based on the content type if the message type can't be determined by the 'NServiceBus.EnclosedMessageTypes' header.
        /// </summary>
        public static void DisableMessageTypeInference<T>(this SerializationExtensions<T> config) where T : SerializationDefinition
        {
            Guard.AgainstNull(nameof(config), config);

            config.EndpointConfigurationSettings.Set(DisableMessageTypeInferenceKey, true);
        }

        /// <summary>
        /// Disables dynamic type loading via <see cref="System.Type.GetType(string)"/> to prevent loading of assemblies for types passed in message header `NServiceBus.EnclosedMessageTypes` to only allow message types during deserialization that were explicitly loaded.  
        /// </summary>
        public static void DisableDynamicTypeLoading<T>(this SerializationExtensions<T> config) where T : SerializationDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            config.EndpointConfigurationSettings.Set(DisableDynamicTypeLoadingKey, true);
        }

        internal static bool IsDynamicTypeLoadingEnabled(this IReadOnlySettings endpointConfigurationSettings) =>
            !endpointConfigurationSettings.GetOrDefault<bool>(DisableDynamicTypeLoadingKey);

        internal static bool IsMessageTypeInferenceEnabled(this IReadOnlySettings endpointConfigurationSettings) =>
            !endpointConfigurationSettings.GetOrDefault<bool>(DisableMessageTypeInferenceKey);

        const string DisableMessageTypeInferenceKey = "NServiceBus.Serialization.DisableMessageTypeInference";
        const string DisableDynamicTypeLoadingKey = "NServiceBus.Serialization.DisableDynamicTypeLoading";
    }
}