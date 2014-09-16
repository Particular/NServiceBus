namespace NServiceBus
{
    using System;

    /// <summary>
    /// Class used to represent an encrypted value with an initialization vector.
    /// </summary>
    [Serializable]
    public class EncryptedValue
    {
        /// <summary>
        /// The encrypted value represented as a Base64 string.
        /// </summary>
        public string EncryptedBase64Value { get; set; }

        /// <summary>
        /// The initialization vector represented as a Base64 string.
        /// </summary>
        public string Base64Iv { get; set; }
    }
}