#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Configuration.AdvancedExtensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Transport;

/// <summary>
/// Extension methods to register NServiceBus endpoints with the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an NServiceBus endpoint with the dependency injection container, enabling the endpoint
    /// to resolve services from the application's service provider and participate in the hosted service lifecycle.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the endpoint to.</param>
    /// <param name="endpointConfiguration">The <see cref="EndpointConfiguration"/> defining how the endpoint should be configured.</param>
    /// <param name="endpointIdentifier">
    /// An optional identifier that uniquely identifies this endpoint within the dependency injection container.
    /// When multiple endpoints are registered (by calling this method multiple times), this parameter is required
    /// and must be a well-defined value that serves as a keyed service identifier.
    /// <para>
    /// In most scenarios, using the endpoint name as the identifier is a good choice.
    /// </para>
    /// <para>
    /// For more complex scenarios such as multi-tenant applications where endpoint infrastructure
    /// per tenant is dynamically resolved, the identifier can be any object that implements <see cref="object.Equals(object?)"/>
    /// and <see cref="object.GetHashCode"/> in a way that conforms to Microsoft Dependency Injection keyed services assumptions.
    /// The key is used with keyed service registration methods like <c>AddKeyedSingleton</c> and related methods,
    /// and can be retrieved using keyed service resolution APIs like <c>GetRequiredKeyedService</c> or
    /// the <c>[FromKeyedServices]</c> attribute on constructor parameters.
    /// </para>
    /// </param>
    /// <remarks>
    /// <para>
    /// When using a keyed endpoint, all services resolved within NServiceBus extension points
    /// (message handlers, sagas, features, installers, etc.) are automatically resolved as keyed services
    /// for that endpoint and do not require the <c>[FromKeyedServices]</c> attribute.
    /// Conversely, the <c>[FromKeyedServices]</c> attribute is required when accessing endpoint-specific services
    /// (such as <see cref="IMessageSession"/>) outside of NServiceBus extension points, for example in controllers
    /// or background jobs.
    /// </para>
    /// <para>
    /// By default, only endpoint-specific registrations are resolved when resolving all services of a given type
    /// within an endpoint. However, for advanced scenarios where global services registered on the shared
    /// service collection need to be resolved along with endpoint-specific ones, use <see cref="KeyedServiceKey.Any"/>
    /// with the <c>[FromKeyedServices]</c> attribute (for example: <c>[FromKeyedServices(KeyedServiceKey.Any)] IEnumerable&lt;IMyService&gt;</c>).
    /// This bypasses the default safeguards that isolate endpoints, allowing resolution of all services including
    /// globally shared ones.
    /// </para>
    /// </remarks>
    public static void AddNServiceBusEndpoint(this IServiceCollection services, EndpointConfiguration endpointConfiguration, object? endpointIdentifier = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpointConfiguration);

        var settings = endpointConfiguration.GetSettings();
        // Unfortunately we have to also check this here due to the multiple hosting variants as long as
        // the old hosting is still supported.
        settings.AssertNotReused();

        var endpointName = endpointConfiguration.EndpointName;
        var transport = settings.Get<TransportSeam.Settings>().TransportDefinition;
        var endpointRegistrations = GetExistingRegistrations(services);
        var hostingSettings = settings.Get<HostingComponent.Settings>();

        ValidateEndpointIdentifier(endpointIdentifier, endpointRegistrations);
        ValidateAssemblyScanning(endpointConfiguration, endpointName, endpointRegistrations);
        ValidateTransportReuse(transport, endpointRegistrations);

        hostingSettings.ConfigureHostLogging(endpointIdentifier);

        if (endpointIdentifier is null)
        {
            // Deliberately creating it here to make sure we are not accidentally doing it too late.
            var externallyManagedContainerHost = EndpointExternallyManaged.Create(endpointConfiguration, services);

            services.AddSingleton(externallyManagedContainerHost);
            services.AddSingleton<IEndpointLifecycle>(sp => new BaseEndpointLifecycle(externallyManagedContainerHost, sp));
            services.AddSingleton<IHostedService, EndpointHostedService>(sp => new EndpointHostedService(sp.GetRequiredService<IEndpointLifecycle>(), sp.GetService<InstallersOptions>()));
        }
        else
        {
            // Backdoor for acceptance testing
            var keyedServices = settings.GetOrDefault<KeyedServiceCollectionAdapter>() ?? new KeyedServiceCollectionAdapter(services, endpointIdentifier);
            var baseKey = keyedServices.ServiceKey.BaseKey;

            // Deliberately creating it here to make sure we are not accidentally doing it too late.
            var externallyManagedContainerHost = EndpointExternallyManaged.Create(endpointConfiguration, keyedServices);

            services.AddKeyedSingleton(baseKey, externallyManagedContainerHost);
            services.AddKeyedSingleton<IEndpointLifecycle>(baseKey, (sp, _) => new EndpointLifecycle(externallyManagedContainerHost, sp, keyedServices.ServiceKey, keyedServices));
            services.AddSingleton<IHostedService, EndpointHostedService>(sp => new EndpointHostedService(sp.GetRequiredKeyedService<IEndpointLifecycle>(baseKey), sp.GetService<InstallersOptions>()));
        }

        services.AddSingleton(new EndpointRegistration(endpointName, endpointIdentifier, endpointConfiguration.AssemblyScanner().Disable, RuntimeHelpers.GetHashCode(transport)));
        services.TryAddSingleton(new InstallersOptions());
    }

    /// <summary>
    /// Configures the application to run NServiceBus installers for all registered endpoints.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the installers to.</param>
    /// <param name="configure">
    /// An optional callback to configure <see cref="InstallersOptions"/>.
    /// When not provided, the default <see cref="InstallersOptions.ShutdownBehavior"/> is
    /// <see cref="InstallersShutdownBehavior.StopApplication"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// All registered NServiceBus endpoints will run their installers, regardless of whether
    /// <see cref="InstallConfigExtensions.EnableInstallers"/> was called on the endpoint configuration.
    /// The endpoints will not start processing messages.
    /// </para>
    /// <para>
    /// By default, the application is gracefully shut down after installers complete.
    /// Other registered <see cref="IHostedService"/> and <see cref="BackgroundService"/> implementations
    /// may briefly start before shutdown takes effect. Services that cooperatively check the
    /// <see cref="IHostApplicationLifetime.ApplicationStopping"/> cancellation token will gracefully abort.
    /// </para>
    /// <para>
    /// To keep the application running after installers complete, set
    /// <see cref="InstallersOptions.ShutdownBehavior"/> to <see cref="InstallersShutdownBehavior.Continue"/>.
    /// </para>
    /// </remarks>
    public static void AddNServiceBusInstallers(this IServiceCollection services, Action<InstallersOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new InstallersOptions { Enabled = true };
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddHostedService<InstallerCoordinatorHostedService>();
    }

    internal static IServiceCollection Unwrap(this IServiceCollection services) => (services as KeyedServiceCollectionAdapter)?.Inner ?? services;

    static void ValidateEndpointIdentifier(object? endpointIdentifier, List<EndpointRegistration> registrations)
    {
        if (registrations.Count == 0)
        {
            return;
        }

        if (endpointIdentifier is null || registrations.Any(r => r.EndpointIdentifier is null))
        {
            throw new InvalidOperationException(
                "When multiple endpoints are registered, each endpoint must provide an endpointIdentifier.");
        }

        if (registrations.Any(r => Equals(r.EndpointIdentifier, endpointIdentifier)))
        {
            throw new InvalidOperationException(
                $"An endpoint with the identifier '{endpointIdentifier}' has already been registered.");
        }
    }

    static void ValidateAssemblyScanning(EndpointConfiguration endpointConfiguration, string endpointName, List<EndpointRegistration> registrations, string message = "its handlers and sagas using the corresponding registrations methods like AddHandler<T>(), AddSaga<T>() etc")
    {
        if (endpointConfiguration.GetSettings().HasSetting("NServiceBus.Hosting.DisableAssemblyScanningValidation"))
        {
            return;
        }

        var endpoints = registrations
            .Append(new EndpointRegistration(endpointName, null, endpointConfiguration.AssemblyScanner().Disable, 0))
            .ToList();

        if (endpoints.Count <= 1)
        {
            return;
        }

        var endpointsWithScanning = endpoints
            .Where(r => !r.ScanningDisabled)
            .Select(r => r.EndpointName)
            .ToList();

        if (endpointsWithScanning.Count > 0)
        {
            throw new InvalidOperationException(
                $"When multiple endpoints are registered, each endpoint must disable assembly scanning " +
                $"(cfg.AssemblyScanner().Disable = true) and explicitly register {message}. " +
                $"The following endpoints have assembly scanning enabled: {string.Join(", ", endpointsWithScanning.Select(n => $"'{n}'"))}.");
        }
    }

    static void ValidateTransportReuse(TransportDefinition transport, List<EndpointRegistration> registrations)
    {
        var transportHash = RuntimeHelpers.GetHashCode(transport);
        var existingRegistration = registrations.FirstOrDefault(r => r.TransportHashCode == transportHash);
        if (existingRegistration is not null)
        {
            throw new InvalidOperationException(
                $"This transport instance is already used by endpoint '{existingRegistration.EndpointName}'. Each endpoint requires its own transport instance.");
        }
    }

    static List<EndpointRegistration> GetExistingRegistrations(IServiceCollection services) =>
        [.. services
            .Where(d => d.ServiceType == typeof(EndpointRegistration) && d.ImplementationInstance is EndpointRegistration)
            .Select(d => (EndpointRegistration)d.ImplementationInstance!)];

    record EndpointRegistration(string EndpointName, object? EndpointIdentifier, bool ScanningDisabled, int TransportHashCode);
}