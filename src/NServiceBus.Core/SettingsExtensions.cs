namespace NServiceBus;

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
    public static IList<Type> GetAvailableTypes(this IReadOnlySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Get<AssemblyScanningComponent.Configuration>().AvailableTypes;
    }

    /// <summary>
    /// Returns the name of this endpoint.
    /// </summary>
    public static string EndpointName(this IReadOnlySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Get<string>("NServiceBus.Routing.EndpointName");
    }

    /// <summary>
    /// Returns the shared queue name of this endpoint.
    /// </summary>
    public static string EndpointQueueName(this IReadOnlySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.TryGet<ReceiveComponent.Configuration>(out var receiveConfiguration))
        {
            throw new InvalidOperationException("EndpointQueueName isn't available until the endpoint configuration is complete.");
        }

        if (receiveConfiguration.IsSendOnlyEndpoint)
        {
            throw new InvalidOperationException("EndpointQueueName isn't available for send only endpoints.");
        }

        return receiveConfiguration.QueueNameBase;
    }
}