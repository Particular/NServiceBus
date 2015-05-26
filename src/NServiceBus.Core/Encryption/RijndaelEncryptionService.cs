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
namespace NServiceBus.Encryption.Rijndael
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    class RijndaelEncryptionService : IEncryptionService
    {

        byte[] encryptionKey;
        List<byte[]> decryptionKeys;

        public RijndaelEncryptionService(string encryptionKey, List<string> expiredKeys)
        {
            this.encryptionKey = Encoding.ASCII.GetBytes(encryptionKey);
            VerifyEncryptionKey(this.encryptionKey);
            var expiredKeyBytes = expiredKeys.Select(key => Encoding.ASCII.GetBytes(key)).ToList();
            VerifyExpiredKeys(expiredKeyBytes);
            VerifyKeysAreNotTooSimilar(expiredKeyBytes);

            decryptionKeys = new List<byte[]>{this.encryptionKey};
            decryptionKeys.AddRange(expiredKeyBytes);

        }

        void VerifyKeysAreNotTooSimilar(List<byte[]> expiredKeyBytes)
        {
            for (var index = 0; index < expiredKeyBytes.Count; index++)
            {
                var decryption = expiredKeyBytes[index];
                CryptographicException exception = null;
                var encryptedValue = Encrypt("a");
                try
                {
                    Decrypt(encryptedValue, decryption);
                }
                catch (CryptographicException cryptographicException)
                {
                    exception = cryptographicException;
                }
                if (exception == null)
                {
                    var message = string.Format("The new Encryption Key is too similar to the Expired Key at index {0}. This can cause issues when decrypting data. To fix this issue please ensure the new encryption key is not too similar to the existing Expired Keys.", index);
                    throw new Exception(message);
                }
            }
        }


        public string Decrypt(EncryptedValue encryptedValue)
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

        static void VerifyExpiredKeys(List<byte[]> keys)
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
                var bitLength = key.Length*8;
                return rijndael.ValidKeySize(bitLength);
            }
        }
    }
}