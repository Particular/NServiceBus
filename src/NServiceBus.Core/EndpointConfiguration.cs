namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Configuration.AdvancedExtensibility;
using Features;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Settings;

/// <summary>
/// Configuration used to create an endpoint instance.
/// </summary>
public class EndpointConfiguration : ExposeSettings
{
    /// <summary>
    /// Initializes the endpoint configuration builder.
    /// </summary>
    /// <param name="endpointName">The name of the endpoint being configured.</param>
    public EndpointConfiguration(string endpointName)
        : base(new SettingsHolder())
    {
        ValidateEndpointName(endpointName);

        Settings.Set(new SystemEnvironment());
        Settings.Set("NServiceBus.Routing.EndpointName", endpointName);

        Settings.SetDefault("Endpoint.SendOnly", false);
        Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
        Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

        Settings.Set(new AssemblyScanningComponent.Configuration(Settings));
        Settings.Set(new HostingComponent.Settings(Settings));
        Settings.Set(new InstallerComponent.Settings(Settings));
        Settings.Set(new TransportSeam.Settings(Settings));
        Settings.Set(new RoutingComponent.Settings(Settings));
        Settings.Set(new ReceiveComponent.Settings(Settings));
        Settings.Set(new RecoverabilityComponent.Configuration());
        Settings.Set(new ConsecutiveFailuresConfiguration());
        Settings.Set(new EnvelopeComponent.Settings());
        Settings.Set(Pipeline = new PipelineSettings(Settings));

        var featureSettings = new FeatureComponent.Settings();

        featureSettings.EnableFeature<ReceiveStatisticsFeature>();
        featureSettings.EnableFeature<SerializationFeature>();
        featureSettings.EnableFeature<StaticHeaders>();
        featureSettings.EnableFeature<Features.Audit>();
        featureSettings.EnableFeature<MessageCausation>();
        featureSettings.EnableFeature<MessageCorrelation>();
        featureSettings.EnableFeature<DelayedDeliveryFeature>();
        featureSettings.EnableFeature<RootFeature>();
        featureSettings.EnableFeature<LicenseReminder>();
        featureSettings.EnableFeature<Mutators>();
        featureSettings.EnableFeature<TimeToBeReceived>();
        featureSettings.EnableFeature<Features.Sagas>();
        featureSettings.EnableFeature<AutoSubscribe>();
        featureSettings.EnableFeature<InferredMessageTypeEnricherFeature>();
        featureSettings.EnableFeature<MessageDrivenSubscriptions>();
        featureSettings.EnableFeature<NativePublishSubscribeFeature>();
        featureSettings.EnableFeature<SubscriptionMigrationMode>();
        featureSettings.EnableFeature<AutoCorrelationFeature>();
        featureSettings.EnableFeature<PlatformRetryNotifications>();
        featureSettings.EnableFeature<OpenTelemetryFeature>();

        Settings.Set(featureSettings);

        conventionsBuilder = new ConventionsBuilder(Settings);
    }

    /// <summary>
    /// Access to the pipeline configuration.
    /// </summary>
    public PipelineSettings Pipeline { get; }

    /// <summary>
    /// Used to configure components in the container.
    /// </summary>
    public void RegisterComponents(Action<IServiceCollection> registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        Settings.Get<HostingComponent.Settings>().UserRegistrations.Add(registration);
    }

    /// <summary>
    /// Configures the endpoint to be send-only.
    /// </summary>
    public void SendOnly() => Settings.Set("Endpoint.SendOnly", true);

    /// <summary>
    /// Defines the conventions to use for this endpoint.
    /// </summary>
    public ConventionsBuilder Conventions() => conventionsBuilder;

    //This needs to be here since we have downstreams that use reflection to access this property
    internal void TypesToScanInternal(IEnumerable<Type> typesToScan) => Settings.Get<AssemblyScanningComponent.Configuration>().UserProvidedTypes = typesToScan.ToList();

    internal void FinalizeConfiguration(IList<Type> availableTypes)
    {
        Settings.SetDefault(conventionsBuilder.Conventions);

        ActivateAndInvoke<INeedInitialization>(availableTypes, t => t.Customize(this));
#pragma warning disable CS0618 // Type or member is obsolete
        ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(availableTypes, t => t.Run(Settings));
#pragma warning restore CS0618 // Type or member is obsolete
    }

    readonly ConventionsBuilder conventionsBuilder;

    static void ValidateEndpointName(string endpointName)
    {
        if (string.IsNullOrWhiteSpace(endpointName))
        {
            throw new ArgumentException("Endpoint name must not be empty", nameof(endpointName));
        }

        if (endpointName.Contains('@'))
        {
            throw new ArgumentException("Endpoint name must not contain an '@' character.", nameof(endpointName));
        }
    }

    static void ActivateAndInvoke<T>(IList<Type> types, Action<T> action) where T : class =>
        ForAllTypes<T>(types, t =>
        {
            if (!HasDefaultConstructor(t))
            {
                throw new Exception($"Unable to create the type '{t.Name}'. Types implementing '{typeof(T).Name}' must have a public parameterless (default) constructor.");
            }

            var instanceToInvoke = (T)Activator.CreateInstance(t);
            action(instanceToInvoke);
        });

    static bool HasDefaultConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) != null;

    static void ForAllTypes<T>(IEnumerable<Type> types, Action<Type> action) where T : class
    {
        foreach (var type in types.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
        {
            action(type);
        }
    }
}