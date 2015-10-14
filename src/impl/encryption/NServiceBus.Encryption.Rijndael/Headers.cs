namespace NServiceBus.Encryption.Rijndael
{
    /// <summary>
    /// Static class containing headers used by NServiceBus.
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// The identifier for the key used for encryptiong property data.
        /// </summary>
        public const string RijndaelKeyIdentifier = "NServiceBus.RijndaelKeyIdentifier";
    }
}
