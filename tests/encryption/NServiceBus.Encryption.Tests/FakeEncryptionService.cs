namespace NServiceBus.Encryption.Tests
{
    using System;

    public class FakeEncryptionService : IEncryptionService
    {
        readonly string hardcodedValue;

        public FakeEncryptionService(string hardcodedValue)
        {
            this.hardcodedValue = hardcodedValue;
        }

        public EncryptedValue Encrypt(string value)
        {
            return new EncryptedValue
                       {
                           EncryptedBase64Value = hardcodedValue,
                           Base64Iv = "initialization_vector"
                       };
        }

        public string Decrypt(EncryptedValue encryptedValue)
        {
            if(encryptedValue.Base64Iv == "init_vector" && encryptedValue.EncryptedBase64Value == "encrypted_value")
             return "A secret";

            throw new InvalidOperationException("Failed to deencrypt");
        }

    }
}