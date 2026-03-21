#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Configuration.AdvancedExtensibility;
using Microsoft.Extensions.DependencyInjection;
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
    public static void AddNServiceBusEndpoint(
        this IServiceCollection services,
        EndpointConfiguration endpointConfiguration,
        object? endpointIdentifier = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpointConfiguration);

        var settings = endpointConfiguration.GetSettings();
        var endpointName = settings.EndpointName();
        var hostingSettings = settings.Get<HostingComponent.Settings>();
        var transport = settings.Get<TransportSeam.Settings>().TransportDefinition;
        var registrations = GetExistingRegistrations(services);

        ValidateEndpointIdentifier(endpointIdentifier, registrations);
        ValidateAssemblyScanning(endpointConfiguration, endpointName, registrations);
        ValidateTransportReuse(transport, registrations);

        hostingSettings.ConfigureMultiHostLogging(endpointIdentifier is not null, endpointIdentifier);

        if (endpointIdentifier is null)
        {
            // Deliberately creating it here to make sure we are not accidentally doing it too late.
            var externallyManagedContainerHost = EndpointWithExternallyManagedContainer.CreateCore(endpointConfiguration, services);

            services.AddSingleton(externallyManagedContainerHost);
            services.AddSingleton<IEndpointLifecycle>(sp => new BaseEndpointLifecycle(externallyManagedContainerHost, sp));
            services.AddSingleton<IHostedService, EndpointHostedService>(sp => new EndpointHostedService(sp.GetRequiredService<IEndpointLifecycle>()));
        }
        else
        {
            // Backdoor for acceptance testing
            var keyedServices = settings.GetOrDefault<KeyedServiceCollectionAdapter>() ?? new KeyedServiceCollectionAdapter(services, endpointIdentifier);

            // Deliberately creating it here to make sure we are not accidentally doing it too late.
            var externallyManagedContainerHost = EndpointWithExternallyManagedContainer.CreateCore(endpointConfiguration, keyedServices);

            services.AddKeyedSingleton(endpointIdentifier, externallyManagedContainerHost);
            services.AddKeyedSingleton<IEndpointLifecycle>(endpointIdentifier, (sp, _) => new EndpointLifecycle(externallyManagedContainerHost, sp, endpointIdentifier, keyedServices));
            services.AddSingleton<IHostedService, EndpointHostedService>(sp => new EndpointHostedService(sp.GetRequiredKeyedService<IEndpointLifecycle>(endpointIdentifier)));
        }

        services.AddSingleton(new EndpointRegistration(endpointName, endpointIdentifier, endpointConfiguration.AssemblyScanner().Disable, RuntimeHelpers.GetHashCode(transport)));
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

    static void ValidateAssemblyScanning(EndpointConfiguration endpointConfiguration, string endpointName, List<EndpointRegistration> registrations)
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
                $"(cfg.AssemblyScanner().Disable = true) and explicitly register its handlers using AddHandler<T>(). " +
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

    sealed record EndpointRegistration(string EndpointName, object? EndpointIdentifier, bool ScanningDisabled, int TransportHashCode);
}