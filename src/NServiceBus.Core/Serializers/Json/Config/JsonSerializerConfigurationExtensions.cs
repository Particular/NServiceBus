namespace NServiceBus
{
    using Features;
    using Settings;

    public static class JsonSerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the json message serializer
        /// </summary>
        public static Configure Json(this SerializationSettings settings)
        {
            settings.Config.Settings.Set("SelectedSerializer", typeof(JsonSerialization));

            return settings.Config;
        }

        /// <summary>
        /// Enables the bson message serializer
        /// </summary>
        public static Configure Bson(this SerializationSettings settings)
        {
            settings.Config.Settings.Set("SelectedSerializer", typeof(BsonSerialization));

            return settings.Config;
        }
    }
}