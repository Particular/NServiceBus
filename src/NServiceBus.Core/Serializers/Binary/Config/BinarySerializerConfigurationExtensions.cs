namespace NServiceBus
{
    using Features;
    using Settings;

#pragma warning disable 1591
    public static class BinarySerializerConfigurationExtensions
#pragma warning restore 1591
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