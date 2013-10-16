namespace NServiceBus
{
    using System;
    using Features;
    using Serializers.XML.Config;
    using Settings;

    public static class XmlSerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the xml message serializer with the given settings
        /// </summary>
        public static SerializationSettings Xml(this SerializationSettings settings, Action<XmlSerializationSettings> customSettings = null)
        {
            Feature.Enable<XmlSerialization>();

            if (customSettings != null)
                customSettings(new XmlSerializationSettings());

            return settings;
        }
    }
}