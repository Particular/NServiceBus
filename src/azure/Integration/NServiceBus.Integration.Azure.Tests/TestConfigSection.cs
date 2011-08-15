using System.Configuration;

namespace NServiceBus.Integration.Azure.Tests
{
    public class TestConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("StringSetting", IsRequired = true)]
        public string StringSetting
        {
            get
            {
                return (string)this["StringSetting"];
            }
            set
            {
                this["StringSetting"] = value;
            }


        }

        [ConfigurationProperty("IntSetting", IsRequired = false)]
        public int IntSetting
        {
            get
            {
                return (int)this["IntSetting"];
            }
            set
            {
                this["IntSetting"] = value;
            }
        }
    }
}