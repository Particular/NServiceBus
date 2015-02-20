namespace NServiceBus.Config
{
    using System;
    using System.Configuration;
    using NServiceBus.SecondLevelRetries;

    /// <summary>
    /// Configuration options for the SLR feature
    /// </summary>
    public class SecondLevelRetriesConfig : ConfigurationSection
    {
        /// <summary>
        /// Creates an instance of <see cref="SecondLevelRetriesConfig"/>.
        /// </summary>
        public SecondLevelRetriesConfig()
        {
            Properties.Add(new ConfigurationProperty("Enabled", typeof(bool), true));
            Properties.Add(new ConfigurationProperty("TimeIncrease", typeof(TimeSpan), DefaultSecondLevelRetryPolicy.DefaultTimeIncrease, null, new TimeSpanValidator(TimeSpan.Zero, TimeSpan.MaxValue), ConfigurationPropertyOptions.None));
            Properties.Add(new ConfigurationProperty("NumberOfRetries", typeof(int), DefaultSecondLevelRetryPolicy.DefaultNumberOfRetries, null, new IntegerValidator(0, Int32.MaxValue), ConfigurationPropertyOptions.None));
        }

        /// <summary>
        /// True if SLR should be used
        /// </summary>
        public bool Enabled
        {
            get { return (bool)this["Enabled"]; }
            set { this["Enabled"] = value; }
        }

        /// <summary>
        /// Sets the time to increase the delay between retries
        /// </summary>
        public TimeSpan TimeIncrease
        {
            get { return (TimeSpan) this["TimeIncrease"]; }
            set { this["TimeIncrease"] = value; }
        }

        /// <summary>
        /// Sets the number of retries to do before aborting and sending the message to the error queue
        /// </summary>
        public int NumberOfRetries
        {
            get { return (int)this["NumberOfRetries"]; }
            set { this["NumberOfRetries"] = value; }
        }
    }
}