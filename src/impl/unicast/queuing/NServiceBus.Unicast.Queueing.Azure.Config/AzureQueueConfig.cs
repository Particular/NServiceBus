using System;
using System.Configuration;

namespace NServiceBus.Config
{
    public class AzureQueueConfig : ConfigurationSection
    {
        [ConfigurationProperty("BaseUri", IsRequired = false, DefaultValue = "http://queue.core.windows.net")]
        public string BaseUri
        {

            get
            {

                return (string)this["BaseUri"];
            }
            set
            {
                this["BaseUri"] = value;
            }
        }


        [ConfigurationProperty("UsePathStyleUris", IsRequired = false,DefaultValue = false)]
        public bool UsePathStyleUris
        {

            get
            {
                return (bool)this["UsePathStyleUris"];
            }
            set
            {
                this["UsePathStyleUris"] = value;
            }
        }

        [ConfigurationProperty("AccountName", IsRequired = true)]
        public string AccountName
        {

            get
            {

                return (string)this["AccountName"];
            }
            set
            {
                this["AccountName"] = value;
            }
        }

        [ConfigurationProperty("Base64Key", IsRequired = true)]
        public string Base64Key
        {

            get
            {
                return (string)this["Base64Key"];
            }
            set
            {
                this["Base64Key"] = value;
            }
        }
    }
}