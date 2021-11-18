namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;
    using Settings;

    /// <summary>
    /// Provides extensions to the settings holder.
    /// </summary>
    public static partial class SettingsExtensions
    {
        /// <summary>
        /// Gets the list of types available to this endpoint.
        /// </summary>
        public static IList<Type> GetAvailableTypes(this IReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            return settings.Get<AssemblyScanningComponent.Configuration>().AvailableTypes;
        }

        /// <summary>
        /// Returns the name of this endpoint.
        /// </summary>
        public static string EndpointName(this IReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            return settings.Get<string>("NServiceBus.Routing.EndpointName");
        }

        /// <summary>
        /// Returns the transport specific address of the shared queue name of this endpoint.
        /// </summary>
        [ObsoleteEx(
            Message = "Inject the ReceiveAddresses class to access the endpoint's receiving transport addresses at runtime. See the NServiceBus version 8 upgrade guide for further details.",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static string LocalAddress(this IReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<TransportDefinition>(out var transportDefinition))
            {
                throw new InvalidOperationException("LocalAddress isn't available since no transport has been configured yet.");
            }

            if (!settings.TryGet<ReceiveComponent.Configuration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("LocalAddress isn't available since this endpoint is configured to run in send-only mode.");
            }

            return transportDefinition.ToTransportAddress(receiveConfiguration.LocalQueueAddress);
        }

        /// <summary>
        /// Returns the shared queue name of this endpoint.
        /// </summary>
        public static string EndpointQueueName(this IReadOnlySettings settings)
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
        [ObsoleteEx(
            Message = "Inject the ReceiveAddresses class to access the endpoint's receiving transport addresses at runtime. See the NServiceBus version 8 upgrade guide for further details.",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static string InstanceSpecificQueue(this IReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (!settings.TryGet<TransportDefinition>(out var transportDefinition))
            {
                throw new InvalidOperationException("LocalAddress isn't available since no transport has been configured yet.");
            }

            if (!settings.TryGet<ReceiveComponent.Configuration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("Instance-specific queue name isn't available since this endpoint is configured to run in send-only mode.");
            }

            if (receiveConfiguration.InstanceSpecificQueueAddress == null)
            {
                return null;
            }

            return transportDefinition.ToTransportAddress(receiveConfiguration.InstanceSpecificQueueAddress);

        }
    }
}