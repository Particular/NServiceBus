namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    public class SecondLevelRetriesConfig : ConfigurationSection
    {
        [ConfigurationProperty("Enabled", IsRequired = false,DefaultValue = true)]
        public bool Enabled
        {
            get
            {
                return CastTo<bool>(this["Enabled"]);
            }
            set
            {
                this["Enabled"] = value;
            }
        }

        [ConfigurationProperty("TimeIncrease", IsRequired = false,DefaultValue = "00:00:10")]
        public TimeSpan TimeIncrease
        {
            get
            {
                try
                {
                    return (TimeSpan)this["TimeIncrease"];
                }
                catch (Exception)
                {
                    return TimeSpan.MinValue;
                }
                
            }
            set
            {
                this["TimeIncrease"] = value;
            }
        }

        [ConfigurationProperty("NumberOfRetries", IsRequired = false,DefaultValue=3)]
        public int NumberOfRetries
        {
            get
            {
                return CastTo<int>(this["NumberOfRetries"]);
            }
            set
            {
                this["NumberOfRetries"] = value;
            }
        }
        static T CastTo<T>(object value)
        {
            try
            {
                return (T)value;
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}