namespace NServiceBus
{
    using System;
    using Pipeline;

    static class StringConversions
    {
        public static void EncryptValue(this IEncryptionService encryptionService, ref string stringToEncrypt, IOutgoingLogicalMessageContext context)
        {
            var encryptedValue = encryptionService.Encrypt(stringToEncrypt, context);

            stringToEncrypt = $"{encryptedValue.EncryptedBase64Value}@{encryptedValue.Base64Iv}";
        }

        public static void DecryptValue(this IEncryptionService encryptionService, ref string stringToDecrypt, IIncomingLogicalMessageContext context)
        {
            var parts = stringToDecrypt.Split(splitChars, StringSplitOptions.None);

            stringToDecrypt = encryptionService.Decrypt(
                new EncryptedValue
                {
                    EncryptedBase64Value = parts[0],
                    Base64Iv = parts[1]
                },
                context
                );
        }

        static char[] splitChars = { '@' };
    }
}