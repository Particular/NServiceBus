namespace NServiceBus.Core.Tests.Encryption
{
    using System.Configuration;
    using NServiceBus.Config;
    using NUnit.Framework;

    [TestFixture]
    public class RijndaelEncryptionServiceConfigValidationsTests
    {
        [Test]
        public void Should_be_false_when_encryption_key_not_in_expired_keys()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                Key = "Key",
                ExpiredKeys = new RijndaelExpiredKeyCollection
                {
                    new RijndaelExpiredKey{Key="AnotherKey"},
                }
            };

            Assert.IsFalse(RijndaelEncryptionServiceConfigValidations.EncryptionKeyListedInExpiredKeys(section));
        }

        [Test]
        public void Should_be_true_when_encryption_key_in_expirede_keys()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                Key = "Key",
                ExpiredKeys = new RijndaelExpiredKeyCollection
                {
                    new RijndaelExpiredKey{Key="Key"},
                }
            };

            Assert.IsTrue(RijndaelEncryptionServiceConfigValidations.EncryptionKeyListedInExpiredKeys(section));
        }


        [Test]
        public void Should_be_false_when_no_duplicate_key_identifiers()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                KeyIdentifier = "2",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey{ Key="A", KeyIdentifier = "1" },
                    new RijndaelExpiredKey{ Key="B", KeyIdentifier = "3;4" }
                }
            };

            Assert.IsFalse(RijndaelEncryptionServiceConfigValidations.ConfigurationHasDuplicateKeyIdentifiers(section));
        }

        [Test]
        public void Should_be_true_when_duplicate_key_identifier_in_concat_root_and_expired_keys()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                KeyIdentifier = "4;2",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey{ Key="A", KeyIdentifier = "1" },
                    new RijndaelExpiredKey{ Key="B", KeyIdentifier = "3;2" }
                }
            };

            Assert.IsTrue(RijndaelEncryptionServiceConfigValidations.ConfigurationHasDuplicateKeyIdentifiers(section));
        }
        [Test]
        public void Should_be_true_when_duplicate_key_identifer_in_concat_expired_keys()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                KeyIdentifier = "2",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey{ Key="A", KeyIdentifier = "1;4" },
                    new RijndaelExpiredKey{ Key="B", KeyIdentifier = "3;4" }
                }
            };

            Assert.IsTrue(RijndaelEncryptionServiceConfigValidations.ConfigurationHasDuplicateKeyIdentifiers(section));
        }

        [Test]
        public void Should_throw_when_expired_keys_has_duplicate_keys()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                ExpiredKeys =
                {
                    new RijndaelExpiredKey{ Key = "Key" },
                }
            };

            Assert.Throws<ConfigurationErrorsException>(() =>
            {
                section.ExpiredKeys.Add(new RijndaelExpiredKey
                {
                    Key = "Key",
                    KeyIdentifier = "ID"
                });
            }, "The entry 'Key' has already been added.");
        }


        [Test]
        public void Should_be_false_when_key_has_no_whitespace()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                ExpiredKeys =
                {
                    new RijndaelExpiredKey{ Key = "Key" }
                }
            };

            Assert.IsFalse(RijndaelEncryptionServiceConfigValidations.ExpiredKeysHaveWhiteSpace(section));
        }
        [Test]
        public void Should_be_true_when_key_has_whitespace()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                ExpiredKeys =
                {
                    new RijndaelExpiredKey{ Key = " " }
                }
            };

            Assert.IsTrue(RijndaelEncryptionServiceConfigValidations.ExpiredKeysHaveWhiteSpace(section));
        }

        [Test]
        public void Should_be_false_when_key_identifier_in_expired_keys()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                ExpiredKeys =
                {
                    new RijndaelExpiredKey{ KeyIdentifier = "ID", Key = "Key" }
                }
            };

            Assert.IsFalse(RijndaelEncryptionServiceConfigValidations.OneOrMoreExpiredKeysHaveNoKeyIdentifier(section));
        }

        [Test]
        public void Should_be_true_when_key_identifier_not_in_expired_keys()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                ExpiredKeys =
                {
                    new RijndaelExpiredKey { Key = "Key" }
                }
            };

            Assert.IsTrue(RijndaelEncryptionServiceConfigValidations.OneOrMoreExpiredKeysHaveNoKeyIdentifier(section));
        }

    }
}