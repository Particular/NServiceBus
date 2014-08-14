#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Xml>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureXmlSerializer
    {

        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Xml>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure XmlSerializer(this Configure config, string nameSpace = null, bool sanitizeInput = false)
        {
            throw new NotImplementedException();
        }
    }
}
