namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureXmlSerializer
    {
 
        [ObsoleteEx(Replacement = "Configure.Serialization.Xml()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure XmlSerializer(this Configure config, string nameSpace = null, bool sanitizeInput = false)
        {
            Configure.Serialization.Xml(s =>
                {
                    if (sanitizeInput)
                        s.SanitizeInput();

                    if (!string.IsNullOrEmpty(nameSpace))
                        s.Namespace(nameSpace);
                });

            return config;
        }
    }
}
