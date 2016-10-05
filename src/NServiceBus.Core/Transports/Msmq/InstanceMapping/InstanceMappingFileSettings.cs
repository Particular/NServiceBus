namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Features;
    using Settings;

    /// <summary>
    /// Allows configuring file-based instance mappings.
    /// </summary>
    public class InstanceMappingFileSettings : ExposeSettings
    {
        /// <summary>
        /// Creates new instance of <see cref="InstanceMappingFileSettings"/>.
        /// </summary>
        public InstanceMappingFileSettings(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Specifies the interval between data refresh attempts.
        /// The default value is 30 seconds.
        /// </summary>
        /// <param name="refreshInterval">Refresh interval. Valid values must be between 1 second and less than 1 day.</param>
        public InstanceMappingFileSettings RefreshInterval(TimeSpan refreshInterval)
        {
            if (refreshInterval < TimeSpan.FromSeconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(refreshInterval), "Value must be at least 1 second.");
            }
            if (refreshInterval > TimeSpan.FromDays(1))
            {
                throw new ArgumentOutOfRangeException(nameof(refreshInterval), "Value must be less than 1 day.");
            }
            Settings.Set(InstanceMappingFileFeature.CheckIntervalSettingsKey, refreshInterval);
            return this;
        }

        /// <summary>
        /// Specifies the path and file name for the instance mapping XML. The default is <code>instance-mapping.xml</code>.
        /// </summary>
        /// <param name="filePath">The relative or absolute file path to the instance mapping XML file.</param>
        public InstanceMappingFileSettings FilePath(string filePath)
        {
            Guard.AgainstNullAndEmpty(nameof(filePath), filePath);

            Settings.Set(InstanceMappingFileFeature.FilePathSettingsKey, filePath);
            return this;
        }
    }
}