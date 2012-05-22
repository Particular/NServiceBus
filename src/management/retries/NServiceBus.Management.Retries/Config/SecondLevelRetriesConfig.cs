﻿using System;
using System.Configuration;

namespace NServiceBus.Config
{
    public class SecondLevelRetriesConfig : ConfigurationSection
    {
        [ConfigurationProperty("Enabled", IsRequired = false)]
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

        [ConfigurationProperty("TimeIncrease", IsRequired = false)]
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

        [ConfigurationProperty("NumberOfRetries", IsRequired = false)]
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