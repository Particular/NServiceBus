namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config.ConfigurationSource;
    using Settings;

    /// <summary>
    /// Provides extensions to the settings holder.
    /// </summary>
    public static class SettingsExtensions
    {
        /// <summary>
        /// Returns the requested config section using the current configuration source.
        /// </summary>
        public static T GetConfigSection<T>(this ReadOnlySettings settings) where T : class, new()
        {
            Guard.AgainstNull(nameof(settings), settings);
            var typesToScan = settings.GetAvailableTypes();
            var configurationSource = settings.Get<IConfigurationSource>();

            var sectionOverrideTypes = typesToScan.Where(t => !t.IsAbstract && typeof(IProvideConfiguration<T>).IsAssignableFrom(t)).ToArray();
            if (sectionOverrideTypes.Length > 1)
            {
                throw new Exception($"Multiple configuration providers implementing IProvideConfiguration<{typeof(T).Name}> were found: {string.Join(", ", sectionOverrideTypes.Select(s => s.FullName))}. Ensure that only one configuration provider per configuration section is found to resolve this error.");
            }

            if (sectionOverrideTypes.Length == 0)
            {
                return configurationSource.GetConfiguration<T>();
            }

            var sectionOverrideType = sectionOverrideTypes[0];

            IProvideConfiguration<T> sectionOverride;

            if (HasConstructorThatAcceptsSettings(sectionOverrideType))
            {
                sectionOverride = (IProvideConfiguration<T>) Activator.CreateInstance(sectionOverrideType, settings);
            }
            else
            {
                sectionOverride = (IProvideConfiguration<T>) Activator.CreateInstance(sectionOverrideType);
            }

            return sectionOverride.GetConfiguration();
        }

        /// <summary>
        /// Gets the list of types available to this endpoint.
        /// </summary>
        public static IList<Type> GetAvailableTypes(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            return settings.Get<IList<Type>>("TypesToScan");
        }

        /// <summary>
        /// Returns the name of this endpoint.
        /// </summary>
        public static string EndpointName(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            return settings.Get<string>("NServiceBus.Routing.EndpointName");
        }

        /// <summary>
        /// Returns the logical address of this endpoint.
        /// </summary>
        public static LogicalAddress LogicalAddress(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            return settings.Get<LogicalAddress>();
        }

        /// <summary>
        /// Returns the shared queue name of this endpoint.
        /// </summary>
        public static string LocalAddress(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            return settings.Get<string>("NServiceBus.SharedQueue");
        }

        /// <summary>
        /// Returns the instance-specific queue name of this endpoint.
        /// </summary>
        public static string InstanceSpecificQueue(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            return settings.GetOrDefault<string>("NServiceBus.EndpointSpecificQueue");
        }

        static bool HasConstructorThatAcceptsSettings(Type sectionOverrideType)
        {
            return sectionOverrideType.GetConstructor(new[]
            {
                typeof(ReadOnlySettings)
            }) != null;
        }
    }
}