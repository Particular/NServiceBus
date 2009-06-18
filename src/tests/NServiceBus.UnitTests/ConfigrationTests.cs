using System;
using System.Configuration;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.ObjectBuilder;
using NUnit.Framework;

namespace NServiceBus.UnitTests
{
    [TestFixture]
    public class ConfigrationTests
    {
        [Test]
        public void User_supplied_configuration()
        {
            var userConfigSource = new UserConfigurationSource();

            var config = Configure.With()
                .CustomConfigurationSource(userConfigSource);
               
            var configSection = Configure.GetConfigSection<TestConfigurationSection>();

            Assert.AreEqual(configSection.GetType(), typeof(TestConfigurationSection));
        }

        [Test]
        public void Default_configuration_source_should_read_from_appconfig()
        {
            var config = Configure.With();

            var configSection = Configure.GetConfigSection<TestConfigurationSection>();

            Assert.AreEqual(configSection.TestSetting, "test");
        }
    }

    public class TestConfigurationSection : ConfigurationSection 
    {
        [ConfigurationProperty("TestSetting", IsRequired = true)]
        public string TestSetting
        {
            get
            {
                var result = this["TestSetting"] as string;
                if (result != null && result.Length == 0)
                    result = null;

                return result;
            }
            set
            {
                this["TestSetting"] = value;
            }
        }
    }

    public class UserConfigurationSource:IConfigurationSource
    {
        public T GetConfiguration<T>() where T : class
        {
            return new TestConfigurationSection() as T;
        }
    }
}