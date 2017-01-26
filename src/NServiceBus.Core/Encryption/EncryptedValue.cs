namespace NServiceBus
{
    using System;

    /// <summary>
    /// Class used to represent an encrypted value with an initialization vector.
    /// </summary>
    [Serializable]
    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class EncryptedValue
    {
        /// <summary>
        /// The encrypted value represented as a Base64 string.
        /// </summary>
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string EncryptedBase64Value { get; set; }

        /// <summary>
        /// The initialization vector represented as a Base64 string.
        /// </summary>
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string Base64Iv { get; set; }
    }
}