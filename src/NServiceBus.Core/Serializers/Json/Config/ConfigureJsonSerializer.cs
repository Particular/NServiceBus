namespace NServiceBus
{
    using System;

    public static class ConfigureJsonSerializer
    {
        [ObsoleteEx(Replacement = "config.Serialization.Json()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure JsonSerializer(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "config.Serialization.Bson()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure BsonSerializer(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}