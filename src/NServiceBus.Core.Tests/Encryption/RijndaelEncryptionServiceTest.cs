﻿namespace NServiceBus.Core.Tests.Encryption
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
            var service = new RijndaelEncryptionServiceWithFakeBus("encryptionKey", encryptionKey, new[] { encryptionKey });
            var encryptedValue = service.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);
            var decryptedValue = service.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_encrypt_and_decrypt_for_expired_key()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new RijndaelEncryptionServiceWithFakeBus("encryptionKey1", encryptionKey1, new[] { encryptionKey1 });
            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);


            var encryptionKey2 = Encoding.ASCII.GetBytes("vznkynwuvateefgduvsqjsufqfrrfcya");
            var expiredKeys = new List<byte[]>
            {
                encryptionKey2,
                encryptionKey1
            };
            var service2 = new RijndaelEncryptionServiceWithFakeBus("encryptionKey1", encryptionKey2, expiredKeys);

            var decryptedValue = service2.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_throw_when_decrypt_with_wrong_key()
        {
            var usedKey = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            var service1 = new RijndaelEncryptionServiceWithFakeBus("should-be-ignored-in-next-arrange", usedKey, new List<byte[]>());
            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);

            var unusedExpiredKeys = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                Encoding.ASCII.GetBytes("cccccccccccccccccccccccccccccccc")
            };

            var service2 = new RijndaelEncryptionServiceWithFakeBus("should-be-ignored", usedKey, unusedExpiredKeys);

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
            var exception = Assert.Throws<Exception>(() => new RijndaelEncryptionServiceWithFakeBus("keyid", invalidKey, new List<byte[]>()));
            Assert.AreEqual("The encryption key has an invalid length of 10 bytes.", exception.Message);
        }

        [Test]
        public void Should_throw_for_invalid_expired_key()
        {
            var validKey = Encoding.ASCII.GetBytes("adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var expiredKeys = new List<byte[]> { Encoding.ASCII.GetBytes("invalidKey") };
            var exception = Assert.Throws<Exception>(() => new RijndaelEncryptionServiceWithFakeBus("keyid", validKey, expiredKeys));
            Assert.AreEqual("The expired key at index 0 has an invalid length of 10 bytes.", exception.Message);
        }

        [Test]
        public void Encrypt_must_set_header()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new RijndaelEncryptionServiceWithFakeBus("encryptionKey1", encryptionKey1, new List<byte[]>());

            Assert.AreEqual(false, service1.OutgoingKeyIdentifierSet);
            service1.Encrypt("string to encrypt");
            Assert.AreEqual(true, service1.OutgoingKeyIdentifierSet);
        }

        [Test]
        public void Decrypt_using_key_identifier()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new RijndaelEncryptionServiceWithFakeBus("encryptionKey1", encryptionKey1, new List<byte[]>());
            var encryptedValue = service1.Encrypt("string to encrypt");

            var expiredKeys = new List<byte[]> { Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6") };
            var service2 = new RijndaelEncryptionServiceWithFakeBus("encryptionKey1", encryptionKey1, expiredKeys)
            {
                IncomingKeyIdentifier = "encryptionKey1"
            };

            var decryptedValue = service2.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }


        [Test]
        public void Decrypt_using_missing_key_identifier_must_throw()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new RijndaelEncryptionServiceWithFakeBus("encryptionKey1", encryptionKey1, new List<byte[]>());
            var encryptedValue = service1.Encrypt("string to encrypt");

            var encryptionKey2 = Encoding.ASCII.GetBytes("vznkynwuvateefgduvsqjsufqfrrfcya");
            var expiredKeys = new List<byte[]> { Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6") };
            var service2 = new RijndaelEncryptionServiceWithFakeBus("encryptionKey1", encryptionKey2, expiredKeys)
            {
                IncomingKeyIdentifier = "missingKey"
            };

            Assert.Catch<InvalidOperationException>(() =>
            {
                service2.Decrypt(encryptedValue);
            }, "Decryption key not available for key identifier 'missingKey'. Please add this key to the rijndael encryption service configuration. Key identifiers are case sensitive.");
        }

        [Test]
        public void Encrypt_using_missing_key_identifier_must_throw()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new RijndaelEncryptionServiceWithFakeBus(null, encryptionKey1, new List<byte[]>());

            Assert.Catch<InvalidOperationException>(() => service1.Encrypt("string to encrypt"), "It is required to set the rijndael key identifer.");
        }

        class RijndaelEncryptionServiceWithFakeBus : RijndaelEncryptionService
        {
            public bool OutgoingKeyIdentifierSet { get; private set; }
            public string IncomingKeyIdentifier { get; set; }

            public RijndaelEncryptionServiceWithFakeBus(
                string encryptionKeyIdentifier,
                byte[] encryptionKey,
                IList<byte[]> expiredKeys
                )
                : base(new Fakes.FakeBus(), encryptionKeyIdentifier, encryptionKeyIdentifier != null ? new Dictionary<string, byte[]> { { encryptionKeyIdentifier, encryptionKey } } : null, expiredKeys)
            {
            }

            protected override void AddKeyIdentifierHeader()
            {
                OutgoingKeyIdentifierSet = true;
            }

            protected override bool TryGetKeyIdentiferHeader(out string keyIdentifier)
            {
                keyIdentifier = this.IncomingKeyIdentifier;
                return IncomingKeyIdentifier != null;
            }
        }
    }

}