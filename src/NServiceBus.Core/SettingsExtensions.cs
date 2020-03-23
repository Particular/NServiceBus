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
            return settings.Get<AssemblyScanningComponent.Configuration>().AvailableTypes;
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

            if (!settings.TryGet<LogicalAddress>("ReceiveComponent.Legacy.LogicalAddress", out var result))
            {
                throw new InvalidOperationException("LogicalAddress isn't available since this endpoint is configured to run in send-only mode.");
            }

            return result;
        }

        /// <summary>
        /// Returns the shared queue name of this endpoint.
        /// </summary>
        public static string LocalAddress(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<string>("ReceiveComponent.Legacy.LocalAddress", out var result))
            {
                throw new InvalidOperationException("LocalAddress isn't available since this endpoint is configured to run in send-only mode.");
            }

            return result;
        }

        /// <summary>
        /// Returns the instance-specific queue name of this endpoint.
        /// </summary>
        public static string InstanceSpecificQueue(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<string>("ReceiveComponent.Legacy.InstanceSpecificQueue", out var result))
            {
                throw new InvalidOperationException("Instance-specific queue name isn't available since this endpoint is configured to run in send-only mode.");
            }

            return result;
        }
    }
}