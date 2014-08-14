/**
https://github.com/hibernating-rhinos/rhino-esb/blob/master/license.txt
Copyright (c) 2005 - 2009 Ayende Rahien (ayende@ayende.com)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.
    * Neither the name of Ayende Rahien nor the names of its
    contributors may be used to endorse or promote products derived from this
    software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/
namespace NServiceBus.Encryption.Rijndael
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using Logging;

    /// <summary>
    /// Implementation of the encryption capability using Rijndael. allowable under the Apache 2.0 license.
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
