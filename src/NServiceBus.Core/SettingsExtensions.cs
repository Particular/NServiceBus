namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Settings;
    using Transport;

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

        //TODO this one might be a big breaking change
        /// <summary>
        /// Returns the transport specific address of the shared queue name of this endpoint.
        /// </summary>
        public static QueueAddress LocalAddress(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<ReceiveComponent.Configuration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("LocalAddress isn't available since this endpoint is configured to run in send-only mode.");
            }

            return receiveConfiguration.LocalAddress;
        }

        /// <summary>
        /// Returns the shared queue name of this endpoint.
        /// </summary>
        public static string EndpointQueueName(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<ReceiveComponent.Configuration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("LocalAddress isn't available since this endpoint is configured to run in send-only mode.");
            }

            return receiveConfiguration.QueueNameBase;
        }

        /// <summary>
        /// Returns the instance-specific queue name of this endpoint.
        /// </summary>
        public static QueueAddress InstanceSpecificQueue(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<ReceiveComponent.Configuration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("Instance-specific queue name isn't available since this endpoint is configured to run in send-only mode.");
            }

            return receiveConfiguration.InstanceSpecificQueue;
        }
    }
}