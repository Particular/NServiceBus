namespace NServiceBus
{
    using Features;
    using Settings;

    public static class JsonSerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the json message serializer
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static SerializationSettings Json(this SerializationSettings settings)
        {
            Feature.Enable<JsonSerialization>();

            return settings;
        }

        /// <summary>
        /// Enables the bson message serializer
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static SerializationSettings Bson(this SerializationSettings settings)
        {
            Feature.Enable<BsonSerialization>();

            return settings;
        }
    }
}