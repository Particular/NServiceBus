namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config.ConfigurationSource;
    using Settings;

    public static class SettingExtentions
    {
        /// <summary>
        ///     Returns the requested config section using the current configuration source.
        /// </summary>
        public static T GetConfigSection<T>(this ReadOnlySettings settings) where T : class, new()
        {
            var typesToScan = settings.GetAvailableTypes();
            var configurationSource = settings.Get<IConfigurationSource>();

            // ReSharper disable HeapView.SlowDelegateCreation
            var sectionOverrideType = typesToScan.FirstOrDefault(t => typeof(IProvideConfiguration<T>).IsAssignableFrom(t));
            // ReSharper restore HeapView.SlowDelegateCreation

            if (sectionOverrideType == null)
            {
                return configurationSource.GetConfiguration<T>();
            }

            var sectionOverride = (IProvideConfiguration<T>)Activator.CreateInstance(sectionOverrideType);

            return sectionOverride.GetConfiguration();
        }

        /// <summary>
        /// Gets the list of types available to this endpoint
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAvailableTypes(this ReadOnlySettings settings)
        {
            return settings.Get<IEnumerable<Type>>("TypesToScan");
        }
    }
}