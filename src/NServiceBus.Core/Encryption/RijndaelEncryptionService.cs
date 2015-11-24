//https://github.com/hibernating-rhinos/rhino-esb/blob/master/license.txt
//Copyright (c) 2005 - 2009 Ayende Rahien (ayende@ayende.com)
//All rights reserved.

//Redistribution and use in source and binary forms, with or without modification,
//are permitted provided that the following conditions are met:

//    * Redistributions of source code must retain the above copyright notice,
//    this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
//    * Neither the name of Ayende Rahien nor the names of its
//    contributors may be used to endorse or promote products derived from this
//    software without specific prior written permission.

//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
//FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
//DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
//CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
//THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
namespace NServiceBus.Encryption.Rijndael
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using NServiceBus.Logging;

    class RijndaelEncryptionService : IEncryptionService
    {
        static readonly ILog Log = LogManager.GetLogger<RijndaelEncryptionService>();
        readonly IBus bus;
        readonly string encryptionKeyIdentifier;
        byte[] encryptionKey;
        IDictionary<string, byte[]> keys;
        IList<byte[]> decryptionKeys; // Required, as we decrypt in the configured order.

        public RijndaelEncryptionService(
            IBus bus,
            string encryptionKeyIdentifier,
            IDictionary<string, byte[]> keys,
            IList<byte[]> decryptionKeys
            )
        {
            this.bus = bus;
            this.encryptionKeyIdentifier = encryptionKeyIdentifier;
            this.decryptionKeys = decryptionKeys;
            this.keys = keys;

            if (string.IsNullOrEmpty(encryptionKeyIdentifier))
            {
                Log.Error("No encryption key identifier configured. Messages with encrypted properties will fail to send. Please add an encryption key identifier to the rijndael encryption service configuration.");
            }
            else if (!keys.TryGetValue(encryptionKeyIdentifier, out encryptionKey))
            {
                throw new ArgumentException("No encryption key for given encryption key identifier.", "encryptionKeyIdentifier");
            }
            else
            {
                VerifyEncryptionKey(encryptionKey);
            }

            VerifyExpiredKeys(decryptionKeys);

            if (encryptionKeyIdentifier != null)
                AddKeyIdentifierHeader();
        }

        public string Decrypt(EncryptedValue encryptedValue)
        {
            string keyIdentifier;

            if (TryGetKeyIdentifierHeader(out keyIdentifier))
            {
                return DecryptUsingKeyIdentifier(encryptedValue, keyIdentifier);
            }
            else
            {
                Log.WarnFormat("Encrypted message has no '" + Headers.RijndaelKeyIdentifier + "' header. Possibility of data corruption. Please upgrade endpoints that send message with encrypted properties.");
                return DecryptUsingAllKeys(encryptedValue);
            }
        }

        string DecryptUsingKeyIdentifier(EncryptedValue encryptedValue, string keyIdentifier)
        {
            byte[] decryptionKey;

            if (!keys.TryGetValue(keyIdentifier, out decryptionKey))
            {
                throw new InvalidOperationException("Decryption key not available for key identifier '" + keyIdentifier + "'. Please add this key to the rijndael encryption service configuration. Key identifiers are case sensitive.");
            }

            try
            {
                return Decrypt(encryptedValue, decryptionKey);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Unable to decrypt property using configured decryption key specified in key identifier header.", ex);
            }
        }

        string DecryptUsingAllKeys(EncryptedValue encryptedValue)
        {
            var cryptographicExceptions = new List<CryptographicException>();

            foreach (var key in decryptionKeys)
            {
                try
                {
                    return Decrypt(encryptedValue, key);
                }
                catch (CryptographicException exception)
                {
                    cryptographicExceptions.Add(exception);
                }
            }
            var message = string.Format("Could not decrypt message. Tried {0} keys.", decryptionKeys.Count);
            throw new AggregateException(message, cryptographicExceptions);
        }

        static string Decrypt(EncryptedValue encryptedValue, byte[] key)
        {
            using (var rijndael = new RijndaelManaged())
            {
                var encrypted = Convert.FromBase64String(encryptedValue.EncryptedBase64Value);
                rijndael.IV = Convert.FromBase64String(encryptedValue.Base64Iv);
                rijndael.Mode = CipherMode.CBC;
                rijndael.Key = key;
                using (var decryptor = rijndael.CreateDecryptor())
                using (var memoryStream = new MemoryStream(encrypted))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public EncryptedValue Encrypt(string value)
        {
            if (string.IsNullOrEmpty(encryptionKeyIdentifier))
            {
                throw new InvalidOperationException("It is required to set the rijndael key identifer.");
            }

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = encryptionKey;
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

        static void VerifyExpiredKeys(IList<byte[]> keys)
        {
            for (var index = 0; index < keys.Count; index++)
            {
                var key = keys[index];
                if (IsValidKey(key))
                {
                    continue;
                }
                var message = string.Format("The expired key at index {0} has an invalid length of {1} bytes.", index, key.Length);
                throw new Exception(message);
            }
        }

        static void VerifyEncryptionKey(byte[] key)
        {
            if (IsValidKey(key))
            {
                return;
            }
            var message = string.Format("The encryption key has an invalid length of {0} bytes.", key.Length);
            throw new Exception(message);
        }

        static bool IsValidKey(byte[] key)
        {
            using (var rijndael = new RijndaelManaged())
            {
                var bitLength = key.Length * 8;
                return rijndael.ValidKeySize(bitLength);
            }
        }

        protected virtual void AddKeyIdentifierHeader()
        {
            bus.OutgoingHeaders[Headers.RijndaelKeyIdentifier] = encryptionKeyIdentifier;
        }

        protected virtual bool TryGetKeyIdentifierHeader(out string keyIdentifier)
        {
            return bus.CurrentMessageContext.Headers.TryGetValue(Headers.RijndaelKeyIdentifier, out keyIdentifier);
        }
    }
}
