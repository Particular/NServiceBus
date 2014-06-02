namespace NServiceBus
{
    using System;
    using Features;
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
        public static Configure Xml(this SerializationSettings settings, Action<XmlSerializationSettings> customSettings = null)
        {
            settings.Config.Features(f=>f.Enable<XmlSerialization>());

            settings.Config.Settings.Set("SelectedSerializer", typeof(XmlSerialization));

            if (customSettings != null)
                customSettings(new XmlSerializationSettings());

            return settings.Config;
        }
    }
}