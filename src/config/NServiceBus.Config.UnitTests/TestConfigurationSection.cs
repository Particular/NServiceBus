using System.Configuration;

namespace NServiceBus.Config.UnitTests
{
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
}