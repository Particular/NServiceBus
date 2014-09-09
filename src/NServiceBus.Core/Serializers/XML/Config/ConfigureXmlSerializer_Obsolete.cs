#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        Message = "Use `configuration.UseSerialization<XmlSerializer>()`, where `configuration` is an instance of type `BusConfiguration`.", 
        RemoveInVersion = "6.0",
        TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureXmlSerializer
    {

        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<XmlSerializer>()`, where `configuration` is an instance of type `BusConfiguration`.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static Configure XmlSerializer(this Configure config, string nameSpace = null, bool sanitizeInput = false)
        {
            throw new NotImplementedException();
        }
    }
}
