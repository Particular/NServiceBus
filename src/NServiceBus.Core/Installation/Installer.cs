#nullable enable

namespace NServiceBus.Installation;

using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using Particular.Obsoletes;

/// <summary>
/// Provides methods to setup an NServiceBus endpoint.
/// </summary>
[ObsoleteMetadata(
    Message = "Self-hosting an installer is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the installation lifecycle of your endpoint",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12",
    ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpointInstaller")]
[Obsolete("Self-hosting an installer is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the installation lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpointInstaller' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public static class Installer
{
    /// <summary>
    /// Executes all the installers and transport configuration without starting the endpoint.
    /// <see cref="Setup"/> always runs installers, even if <see cref="InstallConfigExtensions.EnableInstallers"/> has not been configured.
    /// </summary>
    [ObsoleteMetadata(
        Message = "Self-hosting an installer is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the installation lifecycle of your endpoint",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpointInstaller")]
    [Obsolete("Self-hosting an installer is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the installation lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpointInstaller' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static async Task Setup(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // does not overwrite installer usernames configured by the user.
        configuration.EnableInstallers();

        var serviceCollection = new ServiceCollection();

        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        await using var provider = serviceProvider.ConfigureAwait(false);

        var creationStrategy = new InternalContainerEndpointCreationStrategy(endpointCreator, NoOpAsyncDisposable.Instance);
        _ = await EndpointPreparation.Prepare(creationStrategy, serviceProvider, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an instance of <see cref="InstallerWithExternallyManagedContainer"/> that can be used to setup an NServiceBus when access to externally registered container dependencies are required.
    /// </summary>
    [ObsoleteMetadata(
        Message = "Self-hosting an installer is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the installation lifecycle of your endpoint",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpointInstaller")]
    [Obsolete("Self-hosting an installer is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the installation lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpointInstaller' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static InstallerWithExternallyManagedContainer CreateInstallerWithExternallyManagedContainer(EndpointConfiguration configuration, IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(serviceCollection);

        // does not overwrite installer usernames configured by the user.
        configuration.EnableInstallers();

        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);

        return new InstallerWithExternallyManagedContainer(endpointCreator);
    }
}