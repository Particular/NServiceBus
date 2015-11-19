namespace NServiceBus.Encryption
{
    using NServiceBus.Pipeline.Contexts;

    interface IEncryptionServiceWithContext : IEncryptionService
    {
        /// <summary>
        /// Encrypts the given value returning an EncryptedValue.
        /// </summary>
        EncryptedValue Encrypt(string value, OutgoingContext context);

        /// <summary>
        /// Decrypts the given EncryptedValue object returning the source string.
        /// </summary>
        string Decrypt(EncryptedValue encryptedValue, IncomingContext context);
    }
}