#nullable enable

namespace NServiceBus.Transport;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    HashSet<IEnabledFeature>? featuresToEnable;

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
    /// Allows a transport to enable a specific feature that will be applied only when the transport is hosted inside a full NServiceBus endpoint.
    /// This allows providing reacher functionality that extend the hosted scenarios if needed.
    /// </summary>
    /// <remarks>This method needs to be called within the constructor of the transport definition</remarks>
    /// <typeparam name="T">The feature to enable.</typeparam>
    protected void EnableHostFeature<T>() where T : Feature
    {
        featuresToEnable ??= [];
        featuresToEnable.Add(new EnabledFeature<T>());
    }

    /// <summary>
    /// Initializes all the factories and supported features for the transport. This method is called right before all features
    /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
    /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
    /// provide information anymore at this stage).
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

    internal IReadOnlyCollection<IEnabledFeature> FeaturesToEnable =>
        featuresToEnable is not null ? featuresToEnable : ReadOnlyCollection<IEnabledFeature>.Empty;

    internal interface IEnabledFeature : IEquatable<IEnabledFeature>
    {
        Type FeatureType { get; }

        void Apply(SettingsHolder settings);

        bool IEquatable<IEnabledFeature>.Equals(IEnabledFeature? other) => other != null && FeatureType == other.FeatureType;
    }

    class EnabledFeature<TFeature> : IEnabledFeature
        where TFeature : Feature
    {
        public Type FeatureType => typeof(TFeature);
        public void Apply(SettingsHolder settingsHolder) => settingsHolder.EnableFeature<TFeature>();
    }
}