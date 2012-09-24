namespace NServiceBus.Encryption.Config
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

        public static bool EnsureCompatibilityWithNSB2 { get; set; }
    }
}