﻿namespace NServiceBus.Core.Tests.Encryption
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
                .Select(x=>x.Key)
                .ToList();
            Assert.AreEqual("key1", section.Key);
            Assert.AreEqual(2,keys.Count);
            Assert.Contains("key2",keys);
            Assert.Contains("key3",keys);
        }

        static T ReadSectionFromText<T>(string s) where T: ConfigurationSection
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
                return (T) configuration.GetSection(typeof(T).Name);
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
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has an empty a 'Key' property.", exception.Message);
        }

        [Test]
        public void Should_throw_for_whitespace_key_in_config()
        {
            var config = new RijndaelEncryptionServiceConfig
            {
                Key = " "
            };
            var exception = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.ConvertConfigToRijndaelService(config));
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
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no data.", exception.Message);
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
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no data.", exception.Message);
        }

        [Test]
        public void Should_for_duplicate_between_key_and_keys_in_config()
        {
            var config = new RijndaelEncryptionServiceConfig
            {
                Key = "a",
                ExpiredKeys = new RijndaelExpiredKeyCollection
                {
                    new RijndaelExpiredKey
                    {
                        Key = "a"
                    }
                }
            };
            var exception = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.ExtractExpiredKeysFromConfigSection(config));
            Assert.AreEqual("The RijndaelEncryptionServiceConfig has a 'Key' that is also defined inside the 'ExpiredKeys'.", exception.Message);
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

            Assert.That(new[]{"a"}, Is.EquivalentTo(keys));
        }
    }

}