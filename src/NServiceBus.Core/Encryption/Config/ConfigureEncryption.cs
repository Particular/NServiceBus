namespace NServiceBus.Encryption.Config
{
    public static class ConfigureEncryption
    {
        /// <summary>
        /// Causes the endpoint to no longer send extra data to make encryption compatible with NSB 2.X
        /// </summary>
        /// <returns></returns>
        [ObsoleteEx(Message = "Moved to NServiceBus namespace.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]        
        public static Configure DisableCompatibilityWithNSB2(this Configure config)
        {
            NServiceBus.ConfigureEncryption.EnsureCompatibilityWithNSB2 = false;
            return config;
        }
    }
}

namespace NServiceBus
{
    public static class ConfigureEncryption
    {
        static ConfigureEncryption()
        {
            EnsureCompatibilityWithNSB2 = true;
        }

        /// <summary>
        /// Causes the endpoint to no longer send extra data to make encryption compatible with NSB 2.X
        /// </summary>
        /// <returns></returns>
        public static Configure DisableCompatibilityWithNSB2(this Configure config)
        {
            EnsureCompatibilityWithNSB2 = false;
            return config;
        }

        internal static bool EnsureCompatibilityWithNSB2 { get; set; }
    }
}