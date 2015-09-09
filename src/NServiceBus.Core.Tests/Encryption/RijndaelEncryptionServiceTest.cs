namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using NServiceBus.Encryption.Rijndael;
    using NUnit.Framework;

    [TestFixture]
    public class RijndaelEncryptionServiceTest
    {
        [Test]
        public void Should_encrypt_and_decrypt()
        {
            var encryptionKey = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service = new RijndaelEncryptionService(encryptionKey, new List<byte[]>());
            var encryptedValue = service.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);
            var decryptedValue = service.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_encrypt_and_decrypt_for_expired_key()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new RijndaelEncryptionService(encryptionKey1, new List<byte[]>());
            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);


            var encryptionKey2 = Encoding.ASCII.GetBytes("vznkynwuvateefgduvsqjsufqfrrfcya");
            var expiredKeys = new List<byte[]> { Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6") };
            var service2 = new RijndaelEncryptionService(encryptionKey2, expiredKeys);

            var decryptedValue = service2.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_throw_when_decrypt_with_wrong_key()
        {
            var validKey = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new RijndaelEncryptionService(validKey, new List<byte[]>());
            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);

            var invalidKey = Encoding.ASCII.GetBytes("adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var invalidExpiredKeys = new List<byte[]> { Encoding.ASCII.GetBytes("bdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6") };
            var service2 = new RijndaelEncryptionService(invalidKey, invalidExpiredKeys);

            var exception = Assert.Throws<AggregateException>(() => service2.Decrypt(encryptedValue));
            Assert.AreEqual("Could not decrypt message. Tried 2 keys.", exception.Message);
            Assert.AreEqual(2, exception.InnerExceptions.Count);
            foreach (var inner in exception.InnerExceptions)
            {
                Assert.IsInstanceOf<CryptographicException>(inner);
            }
        }

        [Test]
        public void Should_throw_for_invalid_key()
        {
            var invalidKey = Encoding.ASCII.GetBytes("invalidKey");
            var exception = Assert.Throws<Exception>(() => new RijndaelEncryptionService(invalidKey, new List<byte[]>()));
            Assert.AreEqual("The encryption key has an invalid length of 10 bytes.", exception.Message);
        }

        [Test]
        public void Should_throw_for_invalid_expired_key()
        {
            var validKey = Encoding.ASCII.GetBytes("adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var expiredKeys = new List<byte[]> { Encoding.ASCII.GetBytes("invalidKey") };
            var exception = Assert.Throws<Exception>(() => new RijndaelEncryptionService(validKey, expiredKeys));
            Assert.AreEqual("The expired key at index 0 has an invalid length of 10 bytes.", exception.Message);
        }
    }

}
