namespace NServiceBus
{
    using Pipeline;

    /// <summary>
    /// Abstraction for encryption capabilities.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts the given value returning an EncryptedValue.
        /// </summary>
        EncryptedValue Encrypt(string value, IOutgoingLogicalMessageContext context);

        /// <summary>
        /// Decrypts the given EncryptedValue object returning the source string.
        /// </summary>
        string Decrypt(EncryptedValue encryptedValue, IIncomingLogicalMessageContext context);
    }
}