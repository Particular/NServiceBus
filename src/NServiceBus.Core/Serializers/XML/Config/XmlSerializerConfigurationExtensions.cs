namespace NServiceBus
{
    using System;
    using Features;
    using Serializers.XML.Config;
    using Settings;

    public static class XmlSerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the xml message serializer with the geiven settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="customSettings"></param>
        /// <returns></returns>
        public static SerializationSettings Xml(this SerializationSettings settings, Action<XmlSerializationSettings> customSettings = null)
        {
            Feature.Enable<XmlSerialization>();

            if (customSettings != null)
                customSettings(new XmlSerializationSettings());

            return settings;
        }
    }
}