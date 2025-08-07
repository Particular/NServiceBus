#nullable enable

namespace NServiceBus.Installation;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides methods to setup an NServiceBus endpoint with an externally managed dependency injection container.
/// </summary>
public class InstallerWithExternallyManagedContainer
{
    readonly EndpointCreator endpointCreator;

    internal InstallerWithExternallyManagedContainer(EndpointCreator endpointCreator) => this.endpointCreator = endpointCreator;

    /// <summary>
    /// Executes all the installers and transport configuration without starting the endpoint.
    /// Installers are always run, even if <see cref="InstallConfigExtensions.EnableInstallers"/> has not been configured.
    /// </summary>
    public async Task Setup(IServiceProvider builder, CancellationToken cancellationToken = default)
    {
        var endpoint = endpointCreator.CreateStartableEndpoint(builder, serviceProviderIsExternallyManaged: true);
        await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
        await endpoint.Setup(cancellationToken).ConfigureAwait(false);
    }
}