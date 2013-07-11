namespace NServiceBus
{
    public static class ConfigureJsonSerializer
    {
        [ObsoleteEx(Replacement = "Configure.Serialization.Json()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure JsonSerializer(this Configure config)
        {
            Configure.Serialization.Json();

            return config;
        }

        [ObsoleteEx(Replacement = "Configure.Serialization.Bson()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure BsonSerializer(this Configure config)
        {
            Configure.Serialization.Bson();
            return config;
        }
    }
}