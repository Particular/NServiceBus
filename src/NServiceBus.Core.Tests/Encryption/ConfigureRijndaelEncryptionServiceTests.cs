namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using NServiceBus.Config;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigureRijndaelEncryptionServiceTests
    {

        [Test]
        public void Should_not_throw_for_empty_keys()
        {
            ConfigureRijndaelEncryptionService.VerifyKeys(new List<string>());
        }

        [Test]
        public void Can_read_from_xml()
        {
            var xml =
@"<?xml version='1.0' encoding='utf-8' standalone='yes'?>
<configuration>
    <configSections>
        <section 
            name='RijndaelEncryptionServiceConfig' 
            type='NServiceBus.Config.RijndaelEncryptionServiceConfig, NServiceBus.Core'/>
</configSections>
<RijndaelEncryptionServiceConfig Key='key1'>
  <ExpiredKeys>
    <add Key='key2' />
    <add Key='key3' />
  </ExpiredKeys>
</RijndaelEncryptionServiceConfig>
</configuration>";

            var section = ReadSectionFromText<RijndaelEncryptionServiceConfig>(xml);
            var keys = section.ExpiredKeys.Cast<RijndaelExpiredKey>()
                .Select(x => x.Key)
                .ToList();
            Assert.AreEqual("key1", section.Key);
            Assert.AreEqual(2, keys.Count);
            Assert.Contains("key2", keys);
            Assert.Contains("key3", keys);
        }

        static T ReadSectionFromText<T>(string s) where T : ConfigurationSection
        {
            var xml = s.Replace("'", "\"");
            var tempPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempPath, xml);

                var fileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = tempPath
                };

                var configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                return (T)configuration.GetSection(typeof(T).Name);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [Test]
        public void Should_throw_for_overlapping_keys()
        {
            var keys = new List<string>
            {
                "key1",
                "key2",
                "key1"
            };
            var exception = Assert.Throws<ArgumentException>(() => ConfigureRijndaelEncryptionService.VerifyKeys(keys));
            Assert.AreEqual("Overlapping keys defined. Please ensure that no keys overlap.\r\nParameter name: expiredKeys", exception.Message);
        }

        [Test]
        public void Should_throw_for_whitespace_key()
        {
            var keys = new List<string>
            {
                "key1",
                "",
                "key2"
            };
            var exception = Assert.Throws<ArgumentException>(() => ConfigureRijndaelEncryptionService.VerifyKeys(keys));
            Assert.AreEqual("Empty encryption key detected in position 1.\r\nParameter name: expiredKeys", exception.Message);
        }

        [Test]
        public void Should_throw_for_null_key()
        {
            var keys = new List<string>
            {
                "key1",
                null,
                "key2"
            };
            var exception = Assert.Throws<ArgumentException>(() => ConfigureRijndaelEncryptionService.VerifyKeys(keys));
            Assert.AreEqual("Empty encryption key detected in position 1.\r\nParameter name: expiredKeys", exception.Message);
        }

        [Test]
        public void Should_throw_for_no_key_in_config()
        {
            var config = new RijndaelEncryptionServiceConfig();
            var exception = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.ConvertConfigToRijndaelService(null, config));
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has an empty a 'Key' property.", exception.Message);
        }

        [Test]
        public void Should_throw_for_whitespace_key_in_config()
        {
            var config = new RijndaelEncryptionServiceConfig
            {
                Key = " "
            };
            var exception = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.ConvertConfigToRijndaelService(null, config));
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has an empty a 'Key' property.", exception.Message);
        }

        [Test]
        public void Should_throw_for_whitespace_keys_in_config()
        {
            var config = new RijndaelEncryptionServiceConfig
            {
                ExpiredKeys = new RijndaelExpiredKeyCollection
                {
                    new RijndaelExpiredKey
                    {
                        Key = " "
                    }
                }
            };
            var exception = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.ExtractExpiredKeysFromConfigSection(config));
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no 'Key' property set.", exception.Message);
        }

        [Test]
        public void Should_throw_for_null_keys_in_config()
        {
            var config = new RijndaelEncryptionServiceConfig
            {
                ExpiredKeys = new RijndaelExpiredKeyCollection
                {
                    new RijndaelExpiredKey()
                }
            };
            var exception = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.ExtractExpiredKeysFromConfigSection(config));
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no 'Key' property set.", exception.Message);
        }

        [Test]
        public void Duplicates_should_be_skipped()
        {
            var config = new RijndaelEncryptionServiceConfig
            {
                ExpiredKeys = new RijndaelExpiredKeyCollection
                {
                    new RijndaelExpiredKey
                    {
                        Key = "a"
                    },
                    new RijndaelExpiredKey
                    {
                        Key = "a"
                    }
                }
            };
            var keys = ConfigureRijndaelEncryptionService.ExtractExpiredKeysFromConfigSection(config);

            Assert.That(new[] { new KeyValuePair<string, string>(String.Empty, "a") }, Is.EquivalentTo(keys));
        }


        [TestFixture]
        public class ValidationFixture
        {
            [Test]
            public void EncryptionKeyListedInExpiredKeysTest()
            {
                var section = new RijndaelEncryptionServiceConfig();
                var expiredKeys = new List<KeyValuePair<string, string>>();

                Assert.IsFalse(ConfigureRijndaelEncryptionService.Validations.EncryptionKeyListedInExpiredKeys(section, expiredKeys));

                section.Key = "Key";
                expiredKeys.Add(new KeyValuePair<string, string>(null, "Key"));

                Assert.IsTrue(ConfigureRijndaelEncryptionService.Validations.EncryptionKeyListedInExpiredKeys(section, expiredKeys));
            }

            [Test]
            public void ExpiredKeysHasDuplicateKeyIdentifiersTest()
            {
                var expiredKeys = new List<KeyValuePair<string, string>>();
                expiredKeys.Add(new KeyValuePair<string, string>(null, "Key"));

                Assert.IsFalse(ConfigureRijndaelEncryptionService.Validations.ExpiredKeysHasDuplicateKeyIdentifiers(expiredKeys));

                expiredKeys.Add(new KeyValuePair<string, string>(null, "Key1"));

                Assert.IsTrue(ConfigureRijndaelEncryptionService.Validations.ExpiredKeysHasDuplicateKeyIdentifiers(expiredKeys));
            }

            [Test]
            public void ExpiredKeysHaveDuplicateKeysTest()
            {
                var expiredKeys = new List<KeyValuePair<string, string>>();
                expiredKeys.Add(new KeyValuePair<string, string>(null, "Key"));

                Assert.IsFalse(ConfigureRijndaelEncryptionService.Validations.ExpiredKeysHaveDuplicateKeys(expiredKeys));

                expiredKeys.Add(new KeyValuePair<string, string>(null, "Key"));

                Assert.IsTrue(ConfigureRijndaelEncryptionService.Validations.ExpiredKeysHaveDuplicateKeys(expiredKeys));
            }


            [Test]
            public void ExpiredKeysHaveWhiteSpaceTest()
            {
                var expiredKeys = new List<KeyValuePair<string, string>>();
                expiredKeys.Add(new KeyValuePair<string, string>(null, "Key"));

                Assert.IsFalse(ConfigureRijndaelEncryptionService.Validations.ExpiredKeysHaveWhiteSpace(expiredKeys));

                expiredKeys.Add(new KeyValuePair<string, string>(null, ""));

                Assert.IsTrue(ConfigureRijndaelEncryptionService.Validations.ExpiredKeysHaveWhiteSpace(expiredKeys));
            }

            [Test]
            public void OneOrMoreExpiredKeysHaveNoKeyIdentifierTest()
            {
                var expiredKeys = new List<KeyValuePair<string, string>>();
                expiredKeys.Add(new KeyValuePair<string, string>("ID", "Key"));

                Assert.IsFalse(ConfigureRijndaelEncryptionService.Validations.OneOrMoreExpiredKeysHaveNoKeyIdentifier(expiredKeys));

                expiredKeys.Add(new KeyValuePair<string, string>(null, " Key "));

                Assert.IsTrue(ConfigureRijndaelEncryptionService.Validations.OneOrMoreExpiredKeysHaveNoKeyIdentifier(expiredKeys));
            }
        }
    }

}
