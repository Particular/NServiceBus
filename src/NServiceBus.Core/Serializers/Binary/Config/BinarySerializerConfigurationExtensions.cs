namespace NServiceBus
{
    using Features;
    using Settings;

    /// <summary>
    /// Allows <see cref="BinarySerialization"/>  to be configured.
    /// </summary>
    public static class BinarySerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the binary message serializer
        /// </summary>
        public static Configure Binary(this SerializationSettings settings)
        {
            settings.Config.Settings.Set("SelectedSerializer",typeof(BinarySerialization));

            return settings.Config;
        }
    }
}