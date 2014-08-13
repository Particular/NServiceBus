#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus.Serializers.XML.Config
{
    using System;

    [ObsoleteEx(
            Replacement = "Configure.With(b => b.UseSerialization<Xml>(c => c.XmlSettings()))",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
    public class XmlSerializationSettings
    {

        [ObsoleteEx(
            Replacement = "Configure.With(b => b.UseSerialization<Xml>(c => c.XmlSettings()))",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings DontWrapRawXml()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Replacement = "Configure.With(b => b.UseSerialization<Xml>(c => c.XmlSettings()))",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings Namespace(string namespaceToUse)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Replacement = "Configure.With(b => b.UseSerialization<Xml>(c => c.XmlSettings()))",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings SanitizeInput()
        {
            throw new NotImplementedException();
        }
    }
}