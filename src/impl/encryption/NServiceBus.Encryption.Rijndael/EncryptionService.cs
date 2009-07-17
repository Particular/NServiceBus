using System;
using System.IO;
using System.Security.Cryptography;

namespace NServiceBus.Encryption.Rijndael
{
    /// <summary>
    /// Implementation of the encryption capability using Rijndael.
    /// Blatantly copied from https://rhino-tools.svn.sourceforge.net/svnroot/rhino-tools/trunk/esb/Rhino.ServiceBus/Impl/RijndaelEncryptionService.cs
    /// allowable under the Apache 2.0 license.
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        /// <summary>
        /// Symmetric key used for encryption.
        /// </summary>
        public byte[] Key { get; set; }

        string IEncryptionService.Decrypt(EncryptedValue encryptedValue)
        {
            var encrypted = Convert.FromBase64String(encryptedValue.EncryptedBase64Value);
            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = Key;
                rijndael.IV = Convert.FromBase64String(encryptedValue.Base64Iv);
                rijndael.Mode = CipherMode.CBC;

                using (var decryptor = rijndael.CreateDecryptor())
                using (var memoryStream = new MemoryStream(encrypted))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        EncryptedValue IEncryptionService.Encrypt(string value)
        {
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
    }
}
