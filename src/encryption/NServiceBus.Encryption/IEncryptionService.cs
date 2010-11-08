namespace NServiceBus.Encryption
{
    /// <summary>
    /// Abstraction for encryption capabilities.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts the given value returning an EncryptedValue.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        EncryptedValue Encrypt(string value);

        /// <summary>
        /// Decrypts the given EncryptedValue object returning the source string.
        /// </summary>
        /// <param name="encryptedValue"></param>
        /// <returns></returns>
        string Decrypt(EncryptedValue encryptedValue);
    }
}
