namespace NServiceBus.Encryption.Rijndael
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using Logging;

    /// <summary>
    /// Implementation of the encryption capability using Rijndael.
    /// Copied from https://rhino-tools.svn.sourceforge.net/svnroot/rhino-tools/trunk/esb/Rhino.ServiceBus/Impl/RijndaelEncryptionService.cs
    /// allowable under the Apache 2.0 license.
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        /// <summary>
        /// Symmetric key used for encryption.
        /// </summary>
        public byte[] Key { get; set; }

        /// <summary>
        /// Expired keys that are being phased out but still used for decryption
        /// </summary>
        public List<byte[]> ExpiredKeys { get; set; }

        string IEncryptionService.Decrypt(EncryptedValue encryptedValue)
        {
            if (Key == null)
            {
                Logger.Warn("Cannot decrypt because a Key was not configured. Please specify 'RijndaelEncryptionServiceConfig' in your application's configuration file.");
                return encryptedValue.EncryptedBase64Value;
            }

            var decryptionKeys = new List<byte[]>{Key};
            if (ExpiredKeys != null)
            {
                decryptionKeys.AddRange(ExpiredKeys);
            }
            var encrypted = Convert.FromBase64String(encryptedValue.EncryptedBase64Value);
            var cryptographicExceptions = new List<CryptographicException>();
            using (var rijndael = new RijndaelManaged())
            {
                rijndael.IV = Convert.FromBase64String(encryptedValue.Base64Iv);
                rijndael.Mode = CipherMode.CBC;

                foreach (var key in decryptionKeys)
                {
                    rijndael.Key = key;
                    try
                    {
                        return Decrypt(rijndael, encrypted);
                    }
                    catch (CryptographicException exception)
                    {
                        cryptographicExceptions.Add(exception);
                    }
                }
            }
            var message = string.Format("Could not decrypt message. Tried {0} keys.", decryptionKeys.Count);
            throw new AggregateException(message, cryptographicExceptions);
        }
        static string Decrypt(RijndaelManaged rijndael, byte[] encrypted)
        {
            using (var decryptor = rijndael.CreateDecryptor())
            using (var memoryStream = new MemoryStream(encrypted))
            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
            using (var reader = new StreamReader(cryptoStream))
            {
                return reader.ReadToEnd();
            }
        }

        EncryptedValue IEncryptionService.Encrypt(string value)
        {
            if (Key == null)
                throw new InvalidOperationException("Cannot encrypt because a Key was not configured. Please specify 'RijndaelEncryptionServiceConfig' in your application's configuration file.");

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = Key;
                rijndael.Mode = CipherMode.CBC;
                rijndael.GenerateIV();

                using (var encryptor = rijndael.CreateEncryptor())
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(value);
                    writer.Flush();
                    cryptoStream.Flush();
                    cryptoStream.FlushFinalBlock();
                    return new EncryptedValue
                    {
                        EncryptedBase64Value = Convert.ToBase64String(memoryStream.ToArray()),
                        Base64Iv = Convert.ToBase64String(rijndael.IV)
                    };
                }
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (EncryptionService));
    }
}
