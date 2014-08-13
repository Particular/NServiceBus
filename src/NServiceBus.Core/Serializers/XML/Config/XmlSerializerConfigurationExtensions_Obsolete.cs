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
            Replacement = "Configure.With(b => b.UseSerialization<Xml>(c => c.XmlSettings()))", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
// ReSharper disable UnusedParameter.Global
        public static Configure Xml(this SerializationSettings settings, Action<XmlSerializationSettings> customSettings = null)
// ReSharper restore UnusedParameter.Global
        {
           throw new NotImplementedException();
        }

    }
}
