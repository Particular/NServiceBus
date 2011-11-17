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
                           EncryptedBase64Value = hardcodedValue
                       };
        }

        public string Decrypt(EncryptedValue encryptedValue)
        {
            throw new NotImplementedException();
        }
    }
}