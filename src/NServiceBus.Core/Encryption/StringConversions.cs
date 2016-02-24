namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    static class StringConversions
    {
        public static bool IsType(object instance)
        {
            return instance is string;
        }

        public static void EncryptValue(this IEncryptionService encryptionService, ref string stringToEncrypt, IOutgoingLogicalMessageContext context)
        {
            var encryptedValue = encryptionService.Encrypt(stringToEncrypt, context);

            stringToEncrypt = $"{encryptedValue.EncryptedBase64Value}@{encryptedValue.Base64Iv}";
        }

        public static void DecryptValue(this IEncryptionService encryptionService, ref string stringToDecrypt, IIncomingLogicalMessageContext context)
        {
            var parts = stringToDecrypt.Split(new[] { '@' }, StringSplitOptions.None);

            stringToDecrypt = encryptionService.Decrypt(
                new EncryptedValue
                {
                    EncryptedBase64Value = parts[0],
                    Base64Iv = parts[1]
                },
                context
                );
        }
    }
}