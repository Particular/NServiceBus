namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using NServiceBus.Encryption;
    using NServiceBus.Encryption.Rijndael;
    using NUnit.Framework;

    [TestFixture]
    public class EncryptionServiceTests
    {
        [Test]
        public void Should_encrypt_and_decrypt()
        {
            var service = (IEncryptionService)new EncryptionService
            {
                Key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")
            };
            var encryptedValue = service.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);
            var decryptedValue = service.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_encrypt_and_decrypt_for_expired_key()
        {
            var service1 = (IEncryptionService)new EncryptionService
            {
                Key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"),
            };
            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);


            var service2 = (IEncryptionService)new EncryptionService
            {
                Key = Encoding.ASCII.GetBytes("adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"),
                ExpiredKeys = new List<byte[]>
                {
                    Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")
                }
            };

            var decryptedValue = service2.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_throw_when_decrypt_with_wrong_key()
        {
            var service1 = (IEncryptionService)new EncryptionService
            {
                Key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"),
            };
            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);

            var invalidKey = "adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6";
            var invalidExpiredKeys = new List<string> { "bdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6" };

            var service2 = (IEncryptionService)new EncryptionService
            {
                Key = Encoding.ASCII.GetBytes(invalidKey),
                ExpiredKeys = invalidExpiredKeys.Select(s => Encoding.ASCII.GetBytes(s)).ToList()
            };

            var exception = Assert.Throws<AggregateException>(() => service2.Decrypt(encryptedValue));
            Assert.AreEqual("Could not decrypt message. Tried 2 keys.", exception.Message);
            Assert.AreEqual(2, exception.InnerExceptions.Count);
            foreach (var inner in exception.InnerExceptions)
            {
                Assert.IsInstanceOf<CryptographicException>(inner);
            }
        }

        [Test]
        public void Should_throw_when_encrypt_and_decrypt_keys_are_too_similar()
        {
            var key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service = new EncryptionService
            {
                Key = key,
                ExpiredKeys = new List<byte[]> { key } //note that we use the same key to get the code to throw
            };
            var exception = Assert.Throws<Exception>(service.VerifyKeysAreNotTooSimilar);

            Assert.AreEqual("The new Encryption Key is too similar to the Expired Key at index 0. This can cause issues when decrypting data. To fix this issue please ensure the new encryption key is not too similar to the existing Expired Keys.", exception.Message);
        }
    }
}