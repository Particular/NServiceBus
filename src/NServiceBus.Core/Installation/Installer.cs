#nullable enable

namespace NServiceBus.Installation;

using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// Provides methods to setup an NServiceBus endpoint.
/// </summary>
public static class Installer
{
    /// <summary>
    /// Executes all the installers and transport configuration without starting the endpoint.
    /// <see cref="Setup"/> always runs installers, even if <see cref="InstallConfigExtensions.EnableInstallers"/> has not been configured.
    /// </summary>
    public static async Task Setup(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // does not overwrite installer usernames configured by the user.
        configuration.EnableInstallers();

        var serviceCollection = new ServiceCollection();
        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        await using (serviceProvider.ConfigureAwait(false))
        {
            var endpoint = endpointCreator.CreateStartableEndpoint(serviceProvider, serviceProviderIsExternallyManaged: false);
            await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
            await endpoint.Setup(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="InstallerWithExternallyManagedContainer"/> that can be used to setup an NServiceBus when access to externally registered container dependencies are required.
    /// </summary>
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