namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;
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
<RijndaelEncryptionServiceConfig Key='key1' KeyIdentifier='A' KeyFormat='Base64'>
  <ExpiredKeys>
    <add Key='key2' KeyIdentifier='B' KeyFormat='Base64' />
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
            Assert.AreEqual("A", section.KeyIdentifier);
            Assert.AreEqual(KeyFormat.Base64, section.KeyFormat);
            Assert.AreEqual("B", section.ExpiredKeys["key2"].KeyIdentifier);
            Assert.AreEqual(KeyFormat.Base64, section.ExpiredKeys["key2"].KeyFormat, "Expired key KeyFormat");
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
            var exception = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.ConvertConfigToRijndaelService(config));
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has an empty 'Key' property.", exception.Message);
        }

        [Test]
        public void Should_throw_for_whitespace_key_in_config()
        {
            var config = new RijndaelEncryptionServiceConfig
            {
                Key = " "
            };
            var exception = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.ConvertConfigToRijndaelService(config));
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has an empty 'Key' property.", exception.Message);
        }

        [Test]
        public void Should_correctly_parse_key_identifiers_containing_multiple_keys()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                KeyIdentifier = "1",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey
                    {
                        KeyIdentifier = "2",
                        Key = "Key"
                    }
                }
            };

            var keys = ConfigureRijndaelEncryptionService.ExtractKeysFromConfigSection(section);

            ICollection<string> expected = new[]
            {
                "1",
                "2"
            };

            Assert.AreEqual(expected, keys.Keys);
        }

        [Test]
        public void Should_correctly_convert_base64_key()
        {
            byte[] key = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

            var base64 = Convert.ToBase64String(key);

            var section = new RijndaelEncryptionServiceConfig
            {
                Key = base64,
                KeyFormat = KeyFormat.Base64,
                KeyIdentifier = "1",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey
                    {
                        KeyIdentifier = "2",
                        Key = base64,
                        KeyFormat = KeyFormat.Base64
                    }
                }
            };

            var keys = ConfigureRijndaelEncryptionService.ExtractKeysFromConfigSection(section);

            Assert.AreEqual(key, keys["1"], "Key in configuration root incorrectly converted");
            Assert.AreEqual(key, keys["2"], "Key in expired keys incorrectly converted");
        }

        [Test]
        public void Should_correctly_convert_ascii_key()
        {
            var asciiKey = "0123456789123456";

            var key = Encoding.ASCII.GetBytes("0123456789123456");

            var section = new RijndaelEncryptionServiceConfig
            {
                Key = asciiKey,
                KeyFormat = KeyFormat.Ascii,
                KeyIdentifier = "1",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey
                    {
                        KeyIdentifier = "2",
                        Key = asciiKey,
                        KeyFormat = KeyFormat.Ascii
                    }
                }
            };

            var keys = ConfigureRijndaelEncryptionService.ExtractKeysFromConfigSection(section);

            Assert.AreEqual(key, keys["1"], "Key in configuration root incorrectly converted");
            Assert.AreEqual(key, keys["2"], "Key in expired keys incorrectly converted");
        }

        [Test]
        public void Should_correctly_convert_ascii_key_when_no_value()
        {
            const string asciiKey = "0123456789123456";

            var key = Encoding.ASCII.GetBytes("0123456789123456");

            var section = new RijndaelEncryptionServiceConfig
            {
                Key = asciiKey,
                KeyIdentifier = "1",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey
                    {
                        KeyIdentifier = "2",
                        Key = asciiKey
                    }
                }
            };

            var keys = ConfigureRijndaelEncryptionService.ExtractKeysFromConfigSection(section);

            Assert.AreEqual(key, keys["1"], "Key in configuration root incorrectly converted");
            Assert.AreEqual(key, keys["2"], "Key in expired keys incorrectly converted");
        }

        [Test]
        public void Should_have_correct_number_of_extracted_keys_without_key_identifier()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                Key = "a",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey
                    {
                        Key = "b"
                    }
                }
            };

            var result = ConfigureRijndaelEncryptionService.ExtractDecryptionKeysFromConfigSection(section);

            Assert.AreEqual(2, result.Count, "Key count");
        }

        [Test]
        public void Should_have_correct_number_of_extracted_keys_with_empty_key_identifier()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                Key = "a",
                KeyIdentifier = "a;",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey
                    {
                        Key = "b",
                        KeyIdentifier = ";b"
                    }
                }
            };

            var result = ConfigureRijndaelEncryptionService.ExtractDecryptionKeysFromConfigSection(section);

            Assert.AreEqual(2, result.Count, "Key count");
        }

        [Test]
        public void Should_trow_for_duplicate_expiredkeys_key_values()
        {
            Assert.Throws<ConfigurationErrorsException>(() =>
            {
                // ReSharper disable once ObjectCreationAsStatement
                new RijndaelEncryptionServiceConfig
                {
                    ExpiredKeys =
                    {
                        new RijndaelExpiredKey
                        {
                            Key = "b",
                            KeyIdentifier = "1"
                        },
                        new RijndaelExpiredKey
                        {
                            Key = "b",
                            KeyIdentifier = "2"
                        }
                    }
                };
            });
        }

        [Test]
        public void Should_have_correct_number_of_extracted_keys_with_key_identifier()
        {
            var section = new RijndaelEncryptionServiceConfig
            {
                Key = "a",
                KeyIdentifier = "a",
                ExpiredKeys =
                {
                    new RijndaelExpiredKey
                    {
                        Key = "b",
                        KeyIdentifier = "b"
                    }
                }
            };

            var result = ConfigureRijndaelEncryptionService.ExtractDecryptionKeysFromConfigSection(section);

            Assert.AreEqual(2, result.Count, "Key count");
        }
    }
}
