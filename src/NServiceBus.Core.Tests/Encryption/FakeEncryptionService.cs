namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using NServiceBus.Pipeline;

    public class FakeEncryptionService : IEncryptionService
    {
        EncryptedValue hardcodedValue;

        public FakeEncryptionService(EncryptedValue hardcodedValue)
        {
            this.hardcodedValue = hardcodedValue;
        }

        public EncryptedValue Encrypt(string value, IOutgoingLogicalMessageContext context)
        {
            return hardcodedValue;
        }

        public string Decrypt(EncryptedValue encryptedValue, IIncomingLogicalMessageContext context)
        {
            if (encryptedValue.Base64Iv == hardcodedValue.Base64Iv && encryptedValue.EncryptedBase64Value == hardcodedValue.EncryptedBase64Value)
             return "A secret";

            throw new InvalidOperationException("Failed to decrypt");
        }

    }
}
