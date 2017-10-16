namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Settings;

    /// <summary>
    /// Provides extensions to the settings holder.
    /// </summary>
    public static partial class SettingsExtensions
    {
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

            if (!settings.TryGet<ReceiveConfiguration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("LogicalAddress isn't available since this endpoint is configured to run in send only mode.");
            }

            return receiveConfiguration.LogicalAddress;
        }

        /// <summary>
        /// Returns the shared queue name of this endpoint.
        /// </summary>
        public static string LocalAddress(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<ReceiveConfiguration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("LocalAddress isn't available since this endpoint is configured to run in send only mode.");
            }

            return receiveConfiguration.LocalAddress;
        }

        /// <summary>
        /// Returns the instance-specific queue name of this endpoint.
        /// </summary>
        public static string InstanceSpecificQueue(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<ReceiveConfiguration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("Instance specific queue name isn't available since this endpoint is configured to run in send only mode.");
            }

            return receiveConfiguration.InstanceSpecificQueue;
        }
    }
}