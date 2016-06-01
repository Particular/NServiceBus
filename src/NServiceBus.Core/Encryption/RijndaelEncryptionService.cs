// ReSharper disable CommentTypo
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

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using Logging;
    using Pipeline;

    class RijndaelEncryptionService : IEncryptionService
    {
        public RijndaelEncryptionService(
            string encryptionKeyIdentifier,
            IDictionary<string, byte[]> keys,
            IList<byte[]> decryptionKeys
            )
        {
            this.encryptionKeyIdentifier = encryptionKeyIdentifier;
            this.decryptionKeys = decryptionKeys;
            this.keys = keys;

            if (string.IsNullOrEmpty(encryptionKeyIdentifier))
            {
                Log.Error("No encryption key identifier configured. Messages with encrypted properties will fail to send. Add an encryption key identifier to the rijndael encryption service configuration.");
            }
            else if (!keys.TryGetValue(encryptionKeyIdentifier, out encryptionKey))
            {
                throw new ArgumentException("No encryption key for given encryption key identifier.", nameof(encryptionKeyIdentifier));
            }
            else
            {
                VerifyEncryptionKey(encryptionKey);
            }

            VerifyExpiredKeys(decryptionKeys);
        }

        public string Decrypt(EncryptedValue encryptedValue, IIncomingLogicalMessageContext context)
        {
            string keyIdentifier;

            if (TryGetKeyIdentifierHeader(out keyIdentifier, context))
            {
                return DecryptUsingKeyIdentifier(encryptedValue, keyIdentifier);
            }
            Log.Warn($"Encrypted message has no '{Headers.RijndaelKeyIdentifier}' header. Possibility of data corruption. Upgrade endpoints that send message with encrypted properties.");
            return DecryptUsingAllKeys(encryptedValue);
        }

        public EncryptedValue Encrypt(string value, IOutgoingLogicalMessageContext context)
        {
            if (string.IsNullOrEmpty(encryptionKeyIdentifier))
            {
                throw new InvalidOperationException("It is required to set the rijndael key identifier.");
            }

            AddKeyIdentifierHeader(context);

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = encryptionKey;
                rijndael.Mode = CipherMode.CBC;
                GenerateIV(rijndael);

                using (var encryptor = rijndael.CreateEncryptor())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
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
        }

        string DecryptUsingKeyIdentifier(EncryptedValue encryptedValue, string keyIdentifier)
        {
            byte[] decryptionKey;

            if (!keys.TryGetValue(keyIdentifier, out decryptionKey))
            {
                throw new InvalidOperationException($"Decryption key not available for key identifier '{keyIdentifier}'. Add this key to the rijndael encryption service configuration. Key identifiers are case sensitive.");
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
            var message = $"Could not decrypt message. Tried {decryptionKeys.Count} keys.";
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
                {
                    using (var memoryStream = new MemoryStream(encrypted))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (var reader = new StreamReader(cryptoStream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
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
                var message = $"The expired key at index {index} has an invalid length of {key.Length} bytes.";
                throw new Exception(message);
            }
        }

        static void VerifyEncryptionKey(byte[] key)
        {
            if (IsValidKey(key))
            {
                return;
            }
            var message = $"The encryption key has an invalid length of {key.Length} bytes.";
            throw new Exception(message);
        }

        static bool IsValidKey(byte[] key)
        {
            using (var rijndael = new RijndaelManaged())
            {
                var bitLength = key.Length*8;

                var maxValidKeyBitLength = rijndael.LegalKeySizes.Max(keyLength => keyLength.MaxSize);
                if (bitLength < maxValidKeyBitLength)
                {
                    Log.WarnFormat("Encryption key is {0} bits which is less than the maximum allowed {1} bits. Consider using a {1}-bit encryption key to obtain the maximum cipher strength", bitLength, maxValidKeyBitLength);
                }

                return rijndael.ValidKeySize(bitLength);
            }
        }

        protected virtual void AddKeyIdentifierHeader(IOutgoingLogicalMessageContext context)
        {
            context.Headers[Headers.RijndaelKeyIdentifier] = encryptionKeyIdentifier;
        }

        protected virtual bool TryGetKeyIdentifierHeader(out string keyIdentifier, IIncomingLogicalMessageContext context)
        {
            return context.Headers.TryGetValue(Headers.RijndaelKeyIdentifier, out keyIdentifier);
        }

        protected virtual void GenerateIV(RijndaelManaged rijndael)
        {
            rijndael.GenerateIV();
        }

        readonly string encryptionKeyIdentifier;
        IList<byte[]> decryptionKeys; // Required, as we decrypt in the configured order.
        byte[] encryptionKey;
        IDictionary<string, byte[]> keys;
        static readonly ILog Log = LogManager.GetLogger<RijndaelEncryptionService>();
    }
}