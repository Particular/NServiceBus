namespace NServiceBus
{
    using Pipeline;

    /// <summary>
    /// Abstraction for encryption capabilities.
    /// </summary>
    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts the given value returning an EncryptedValue.
        /// </summary>
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        EncryptedValue Encrypt(string value, IOutgoingLogicalMessageContext context);

        /// <summary>
        /// Decrypts the given EncryptedValue object returning the source string.
        /// </summary>
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        string Decrypt(EncryptedValue encryptedValue, IIncomingLogicalMessageContext context);
    }
}