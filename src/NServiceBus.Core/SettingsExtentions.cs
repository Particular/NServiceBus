namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config.ConfigurationSource;
    using Settings;

    /// <summary>
    /// Provides extentions to the settings holder.
    /// </summary>
    public static class SettingsExtentions
    {
        /// <summary>
        ///     Returns the requested config section using the current configuration source.
        /// </summary>
        public static T GetConfigSection<T>(this ReadOnlySettings settings) where T : class, new()
        {
            Guard.AgainstNull("settings", settings);
            var typesToScan = settings.GetAvailableTypes();
            var configurationSource = settings.Get<IConfigurationSource>();

            // ReSharper disable HeapView.SlowDelegateCreation
            var sectionOverrideType = typesToScan.FirstOrDefault(t => typeof(IProvideConfiguration<T>).IsAssignableFrom(t));
            // ReSharper restore HeapView.SlowDelegateCreation

            if (sectionOverrideType == null)
            {
                return configurationSource.GetConfiguration<T>();
            }

            IProvideConfiguration<T> sectionOverride;

            if (HasConstructorThatAcceptsSettings(sectionOverrideType))
            {
                sectionOverride = (IProvideConfiguration<T>)Activator.CreateInstance(sectionOverrideType, settings);
            }
            else
            {
                sectionOverride = (IProvideConfiguration<T>)Activator.CreateInstance(sectionOverrideType);
            }

            return sectionOverride.GetConfiguration();
        }

        /// <summary>
        /// Gets the list of types available to this endpoint.
        /// </summary>
        public static IList<Type> GetAvailableTypes(this ReadOnlySettings settings)
        {
            Guard.AgainstNull("settings", settings);
            return settings.Get<IList<Type>>("TypesToScan");
        }

        /// <summary>
        /// Returns the name of this endpoint.
        /// </summary>
        public static EndpointName EndpointName(this ReadOnlySettings settings)
        {
            Guard.AgainstNull("settings", settings);
            return settings.Get<EndpointName>();
        }
        
        /// <summary>
        /// Returns the name of this instance of the endpoint.
        /// </summary>
        public static EndpointInstanceName EndpointInstanceName(this ReadOnlySettings settings)
        {
            Guard.AgainstNull("settings", settings);
            return settings.Get<EndpointInstanceName>();
        }

        /// <summary>
        /// Returns the queue name of this endpoint.
        /// </summary>
        public static string LocalAddress(this ReadOnlySettings settings)
        {
            Guard.AgainstNull("settings", settings);
            return settings.Get<string>("NServiceBus.LocalAddress");
        }
        
        /// <summary>
        /// Returns the root logical address of this nedpoint.
        /// </summary>
        public static LogicalAddress RootLogicalAddress(this ReadOnlySettings settings)
        {
            Guard.AgainstNull("settings", settings);
            return settings.Get<LogicalAddress>();
        }

        static bool HasConstructorThatAcceptsSettings(Type sectionOverrideType)
        {
            return sectionOverrideType.GetConstructor(new [] { typeof(ReadOnlySettings) }) != null;
        }
    }
}