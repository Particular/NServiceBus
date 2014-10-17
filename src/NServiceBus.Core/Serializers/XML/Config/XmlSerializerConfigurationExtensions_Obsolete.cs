// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using Serializers.XML.Config;
    using Settings;

    /// <summary>
    /// XmlSerializer configuration extensions.
    /// </summary>
    public static class XmlSerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the xml message serializer with the given settings
        /// </summary>
        [ObsoleteEx(
            Message = "Use configuration.UseSerialization<XmlSerializer>(), where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static Configure Xml(this SerializationSettings settings, Action<XmlSerializationSettings> customSettings = null)
        {
           throw new NotImplementedException();
        }

    }
}
