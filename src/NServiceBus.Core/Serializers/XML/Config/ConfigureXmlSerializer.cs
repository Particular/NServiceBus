
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    [ObsoleteEx(Replacement = "config.Serialization.Xml()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureXmlSerializer
    {

        [ObsoleteEx(Replacement = "config.Serialization.Xml()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure XmlSerializer(this Configure config, string nameSpace = null, bool sanitizeInput = false)
        {
            throw new NotImplementedException();
        }
    }
}
