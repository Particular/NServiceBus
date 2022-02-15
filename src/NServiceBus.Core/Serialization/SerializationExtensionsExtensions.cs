namespace NServiceBus.Serialization
{
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

        internal static bool IsMessageTypeInferenceEnabled(this IReadOnlySettings endpointConfigurationSettings) =>
            !endpointConfigurationSettings.GetOrDefault<bool>(DisableMessageTypeInferenceKey);

        const string DisableMessageTypeInferenceKey = "NServiceBus.Serialization.DisableMessageTypeInference";
    }
}