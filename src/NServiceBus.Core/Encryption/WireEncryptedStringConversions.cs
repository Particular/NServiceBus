namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    static class WireEncryptedStringConversions
    {
        public static bool IsType(object instance)
        {
            return instance is WireEncryptedString;
        }

        public static void EncryptValue(this IEncryptionService encryptionService, WireEncryptedString wireEncryptedString, IOutgoingLogicalMessageContext context)
        {
            wireEncryptedString.EncryptedValue = encryptionService.Encrypt(wireEncryptedString.Value, context);
            wireEncryptedString.Value = null;
        }

        public static void DecryptValue(this IEncryptionService encryptionService, WireEncryptedString wireEncryptedString, IIncomingLogicalMessageContext context)
        {
            if (wireEncryptedString.EncryptedValue == null)
            {
                throw new Exception("Encrypted property is missing encryption data");
            }

            wireEncryptedString.Value = encryptionService.Decrypt(wireEncryptedString.EncryptedValue, context);
        }
    }
}