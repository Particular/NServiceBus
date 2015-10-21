namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using System.Collections.Generic;
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
            var key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");

            var service = (IEncryptionService)new FakeEncryptionService(
                "ID",
                new Dictionary<string, byte[]> { { "ID", key } },
                new List<byte[]>()
                )
            {
                IncomingKeyIdentifierHeader = "ID"
            };

            var encryptedValue = service.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);
            var decryptedValue = service.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_encrypt_and_decrypt_for_expired_key()
        {
            var key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");

            var service1 = (IEncryptionService)new FakeEncryptionService(
                "ID",
                new Dictionary<string, byte[]> { { "ID", key } },
                new List<byte[]>()
                );

            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);


            var key2 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");

            var service2 = (IEncryptionService)new FakeEncryptionService(
                "ID",
                new Dictionary<string, byte[]> { { "ID", key } },
                new List<byte[]> { key2 }
                );

            var decryptedValue = service2.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_throw_when_decrypt_with_wrong_key()
        {
            var key = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            var service1 = (IEncryptionService)new FakeEncryptionService(
                "ID",
                new Dictionary<string, byte[]> { { "ID", key } },
                new List<byte[]>()
                );

            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);
            Console.Write(encryptedValue.EncryptedBase64Value);
            Console.Write(encryptedValue.Base64Iv);

            var invalidExpiredKeys = new List<byte[]> { Encoding.ASCII.GetBytes("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb") };

            var service2 = (IEncryptionService)new FakeEncryptionService(
                "ID",
                new Dictionary<string, byte[]> { { "ID", key } },
                invalidExpiredKeys
                );

            var exception = Assert.Throws<AggregateException>(() => service2.Decrypt(encryptedValue));
            Assert.AreEqual("Could not decrypt message. Tried 1 keys.", exception.Message);
            Assert.AreEqual(1, exception.InnerExceptions.Count);
            foreach (var inner in exception.InnerExceptions)
            {
                Assert.IsInstanceOf<CryptographicException>(inner);
            }
        }

        [Test]
        public void Should_throw_when_encrypting_with_no_key_identifier()
        {
            var key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");

            var service1 = (IEncryptionService)new FakeEncryptionService(null, new Dictionary<string, byte[]> { { "ID", key } }, new List<byte[]> { key });

            Assert.Throws<InvalidOperationException>(() => service1.Encrypt("string to encrypt"));
        }

        [Test]
        public void Should_throw_when_decrypting_with_no_matching_key_identifier()
        {
            var key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = (IEncryptionService)new FakeEncryptionService("not-used", new Dictionary<string, byte[]> { { "not-used", key } }, new List<byte[]> { key })
            {
                IncomingKeyIdentifierHeader = "bad",
            };

            var item = new EncryptedValue
            {
                Base64Iv = "JL76xdu4AzEQXg2tNTq55w==",
                EncryptedBase64Value = "yJYi4kUWy5V4KhsWovIQktrIy5aisIrqbX5nbcM8V0M="
            };

            Assert.Throws<InvalidOperationException>(() => service1.Decrypt(item), "KeyIdentifier present but decryption key not configured for given key identifier.");
        }

        [Test]
        public void Should_throw_informative_exception_when_decryption_fails_with_key_identifier()
        {
            var keyIdentier = "encryptionKey1";

            var key1 = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            IEncryptionService service1 = new FakeEncryptionService(keyIdentier, new Dictionary<string, byte[]> { { keyIdentier, key1 } }, new List<byte[]>());
            var encryptedValue = service1.Encrypt("string to encrypt");

            var key2 = Encoding.ASCII.GetBytes("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            IEncryptionService service2 = new FakeEncryptionService(keyIdentier, new Dictionary<string, byte[]> { { keyIdentier, key2 } }, new List<byte[]>())
            {
                IncomingKeyIdentifierHeader = "encryptionKey1"
            };

            Assert.Catch<InvalidOperationException>(() =>
            {
                service2.Decrypt(encryptedValue);
            }, "Unable to decrypt property using configured decryption key specified in key identifier header.");
        }

        class FakeEncryptionService : EncryptionService
        {
            public string IncomingKeyIdentifierHeader { set; private get; }
            public bool OutgoingKeyIdentifierHeaderSet { get; private set; }


            public FakeEncryptionService(
                string encryptionKeyIdentifier,
                IDictionary<string, byte[]> keys,
                List<byte[]> expiredKeys
                )
            {
                EncryptionKeyIdentifier = encryptionKeyIdentifier;
                Keys = keys;
                ExpiredKeys = expiredKeys;
                if (encryptionKeyIdentifier != null)
                {
                    byte[] value;
                    keys.TryGetValue(encryptionKeyIdentifier, out value);
                    Key = value;
                }
            }

            protected override void AddKeyIdentifierHeader()
            {
                OutgoingKeyIdentifierHeaderSet = true;
            }

            protected override bool TryGetKeyIdentifierHeader(out string keyIdentifier)
            {
                keyIdentifier = IncomingKeyIdentifierHeader;
                return IncomingKeyIdentifierHeader != null;
            }
        }
    }
}