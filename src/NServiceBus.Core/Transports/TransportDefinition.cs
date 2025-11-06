#nullable enable

namespace NServiceBus.Transport;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Settings;

/// <summary>
/// Defines a transport.
/// </summary>
public abstract class TransportDefinition
{
    TransportTransactionMode transportTransactionMode;
    List<IEnabled>? featuresToEnable;

    /// <summary>
    /// Creates a new transport definition.
    /// </summary>
    protected TransportDefinition(TransportTransactionMode defaultTransactionMode, bool supportsDelayedDelivery, bool supportsPublishSubscribe, bool supportsTTBR)
    {
        transportTransactionMode = defaultTransactionMode;
        SupportsDelayedDelivery = supportsDelayedDelivery;
        SupportsPublishSubscribe = supportsPublishSubscribe;
        SupportsTTBR = supportsTTBR;
    }

    /// <summary>
    /// Allows a transport to enable a specific feature that will be applied only when the transport is hosted inside an NServiceBus endpoint.
    /// This allows providing richer functionality that extends the hosted scenarios if needed.
    /// </summary>
    /// <remarks>This method needs to be called within the constructor(s) of the transport definition.</remarks>
    /// <typeparam name="T">The feature to enable.</typeparam>
    protected void EnableEndpointFeature<T>() where T : Feature, new()
    {
        featuresToEnable ??= [];
        featuresToEnable.Add(Enabled<T>.Instance);
    }

    /// <summary>
    /// Initializes transport factories and transport-specific behavior.
    /// This method is invoked after transport-provided endpoint features have been registered
    /// and after features have been configured, but before feature
    /// activation/initialization completes and before feature startup tasks run. At this point
    /// the settings holder is finalized with user-provided settings and may be
    /// used for providing transport defaults or reading finalized settings when hosted in an endpoint.
    /// </summary>
    public abstract Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of all supported transaction modes of this transport.
    /// </summary>
    public abstract IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes();

    /// <summary>
    /// Defines the selected TransportTransactionMode for this instance.
    /// </summary>
    public virtual TransportTransactionMode TransportTransactionMode
    {
        get => transportTransactionMode;
        set
        {
            if (!GetSupportedTransactionModes().Contains(value))
            {
                throw new Exception($"Transaction mode {value} is not supported.");
            }
            transportTransactionMode = value;
        }
    }

    /// <summary>
    /// Indicates whether this transport supports delayed delivery natively.
    /// </summary>
    public bool SupportsDelayedDelivery { get; }

    /// <summary>
    /// Indicates whether this transport supports publish-subscribe natively.
    /// </summary>
    public bool SupportsPublishSubscribe { get; }

    /// <summary>
    /// Indicates whether this transport supports time-to-be-received settings for messages.
    /// </summary>
    public bool SupportsTTBR { get; }

    internal void Configure(SettingsHolder settings)
    {
        if (featuresToEnable is null)
        {
            return;
        }

        foreach (var featureToEnable in featuresToEnable)
        {
            featureToEnable.Apply(settings);
        }
    }

    interface IEnabled
    {
        void Apply(SettingsHolder settings);
    }

    sealed class Enabled<TFeature> : IEnabled
        where TFeature : Feature, new()
    {
        Enabled()
        {
        }

        public static readonly IEnabled Instance = new Enabled<TFeature>();

        public void Apply(SettingsHolder settingsHolder) => settingsHolder.EnableFeature<TFeature>();
    }
}