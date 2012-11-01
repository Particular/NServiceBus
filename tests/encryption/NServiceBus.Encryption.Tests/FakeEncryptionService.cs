namespace NServiceBus.Encryption.Tests
{
    using System;

    public class FakeEncryptionService : IEncryptionService
    {
        readonly EncryptedValue hardcodedValue;

        public FakeEncryptionService(EncryptedValue hardcodedValue)
        {
            this.hardcodedValue = hardcodedValue;
        }

        public EncryptedValue Encrypt(string value)
        {
            return hardcodedValue;
        }

        public string Decrypt(EncryptedValue encryptedValue)
        {
            if (encryptedValue.Base64Iv == hardcodedValue.Base64Iv && encryptedValue.EncryptedBase64Value == hardcodedValue.EncryptedBase64Value)
             return "A secret";

            throw new InvalidOperationException("Failed to decrypt");
        }

    }
}