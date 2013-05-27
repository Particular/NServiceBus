namespace NServiceBus
{
    public static class ConfigureJsonSerializer
    {
        [ObsoleteEx(Replacement = "Configure.Serializers.Json()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure JsonSerializer(this Configure config)
        {
            Configure.Serialization.Json();

            return config;
        }

        [ObsoleteEx(Replacement = "Configure.Serializers.Bson()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure BsonSerializer(this Configure config)
        {
            Configure.Serialization.Bson();
            return config;
        }
    }
}