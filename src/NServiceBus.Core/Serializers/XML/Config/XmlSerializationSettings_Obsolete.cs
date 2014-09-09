#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus.Serializers.XML.Config
{
    using System;

    [ObsoleteEx(
            Message = "Use configuration.UseSerialization<XmlSerializer>(), where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
    public class XmlSerializationSettings
    {

        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<XmlSerializer>().DontWrapRawXml()`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings DontWrapRawXml()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<XmlSerializer>().Namespace(namespaceToUse)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings Namespace(string namespaceToUse)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<XmlSerializer>().SanitizeInput()`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings SanitizeInput()
        {
            throw new NotImplementedException();
        }
    }
}