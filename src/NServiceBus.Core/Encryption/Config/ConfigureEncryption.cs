namespace NServiceBus.Encryption.Config
{
    public static class ConfigureEncryption
    {
        /// <summary>
        /// Causes the endpoint to no longer send extra data to make encryption compatible with NSB 2.X
        /// </summary>
        /// <returns></returns>
        [ObsoleteEx(Message = "Not supported anymore.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]        
        public static Configure DisableCompatibilityWithNSB2(this Configure config)
        {
            return config;
        }
    }
}