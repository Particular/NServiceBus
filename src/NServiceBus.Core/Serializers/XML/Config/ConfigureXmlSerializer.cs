
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    [ObsoleteEx(Replacement = "config.UseSerialization<Xml>()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureXmlSerializer
    {

        [ObsoleteEx(Replacement = "config.UseSerialization<Xml>()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
#pragma warning disable 1591
        public static Configure XmlSerializer(this Configure config, string nameSpace = null, bool sanitizeInput = false)
#pragma warning restore 1591
        {
            throw new NotImplementedException();
        }
    }
}
