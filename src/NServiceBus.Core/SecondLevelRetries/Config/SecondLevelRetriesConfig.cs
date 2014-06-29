namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Configuration options for the SLR feature
    /// </summary>
    public class SecondLevelRetriesConfig : ConfigurationSection
    {
        /// <summary>
        /// True if SLR should be used
        /// </summary>
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

        /// <summary>
        /// Sets the time to increase the delay between retries
        /// </summary>
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

        /// <summary>
        /// Sets the number of retries to do before aborting and sending the message to the error queue
        /// </summary>
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