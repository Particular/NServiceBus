namespace NServiceBus
{
    using System;
    using Settings;

#pragma warning disable 1591
    public static class JsonSerializerConfigurationExtensions
#pragma warning restore 1591
    {
        /// <summary>
        /// Enables the json message serializer
        /// </summary>
        [ObsoleteEx(Replacement = "config.UseSerialization<Json>()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
// ReSharper disable UnusedParameter.Global
        public static Configure Json(this SerializationSettings settings)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enables the bson message serializer
        /// </summary>
        [ObsoleteEx(Replacement = "config.UseSerialization<Json>()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
// ReSharper disable UnusedParameter.Global
        public static Configure Bson(this SerializationSettings settings)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }
    }
}