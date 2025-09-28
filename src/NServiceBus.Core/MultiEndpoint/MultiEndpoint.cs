#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Entry point for configuring and starting multiple endpoints that share a single service collection.
/// </summary>
public static class MultiEndpoint
{
    /// <summary>
    /// Configures multiple endpoints on the provided <see cref="IServiceCollection"/> and returns a startable host that assumes the service provider will be externally managed.
    /// </summary>
    public static IStartableMultiEndpointWithExternallyManagedContainer Create(IServiceCollection serviceCollection, Action<MultiEndpointConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new MultiEndpointConfiguration();
        configure(configuration);

        var definitions = configuration.Build();
        if (definitions.Count == 0)
        {
            throw new InvalidOperationException("At least one endpoint must be configured.");
        }

        var endpointHosts = new List<EndpointHost>(definitions.Count);

        foreach (var definition in definitions)
        {
            var keyedServices = new KeyedServiceCollectionAdapter(serviceCollection, definition.ServiceKey);
            var accessor = new EndpointInstanceAccessor();

            keyedServices.AddSingleton<EndpointInstanceAccessor>(accessor);
            keyedServices.AddSingleton(_ => new Lazy<IMessageSession>(() => accessor.Get()));
            keyedServices.AddSingleton<IMessageSession>(_ => accessor.Get());
            keyedServices.AddSingleton<IEndpointInstance>(_ => accessor.Get());

            var endpointCreator = EndpointCreator.Create(definition.Configuration, keyedServices);
            endpointHosts.Add(new EndpointHost(definition.EndpointName, definition.ServiceKey, endpointCreator, keyedServices, accessor));
        }

        return new MultiEndpointHost(endpointHosts);
    }

    /// <summary>
    /// Configures, builds, and starts endpoints on a new <see cref="ServiceCollection"/> managed by the caller.
    /// </summary>
    public static async Task<IMultiEndpointInstance> Start(Action<MultiEndpointConfiguration> configure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var services = new ServiceCollection();
        var startable = Create(services, configure);
        var serviceProvider = services.BuildServiceProvider();

        var instance = await startable.Start(serviceProvider, cancellationToken).ConfigureAwait(false);
        return new OwnedServiceProviderMultiEndpointInstance(instance, serviceProvider);
    }

    sealed class EndpointHost
    {
        public EndpointHost(string endpointName, object serviceKey, EndpointCreator creator, KeyedServiceCollectionAdapter services, EndpointInstanceAccessor accessor)
        {
            EndpointName = endpointName;
            ServiceKey = serviceKey;
            EndpointCreator = creator;
            Services = services;
            Accessor = accessor;
        }

        public string EndpointName { get; }
        public object ServiceKey { get; }
        public EndpointCreator EndpointCreator { get; }
        public KeyedServiceCollectionAdapter Services { get; }
        public EndpointInstanceAccessor Accessor { get; }
    }

    sealed class MultiEndpointHost : IStartableMultiEndpointWithExternallyManagedContainer
    {
        public MultiEndpointHost(IReadOnlyCollection<EndpointHost> endpointHosts)
        {
            this.endpointHosts = endpointHosts;
        }

        public async Task<IMultiEndpointInstance> Start(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            var startedEndpoints = new List<IMultiEndpointInstance.EndpointInstanceInfo>(endpointHosts.Count);

            try
            {
                foreach (var endpoint in endpointHosts)
                {
                    var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, endpoint.ServiceKey, endpoint.Services);
                    var startable = endpoint.EndpointCreator.CreateStartableEndpoint(keyedProvider, serviceProviderIsExternallyManaged: true);

                    await startable.RunInstallers(cancellationToken).ConfigureAwait(false);
                    await startable.Setup(cancellationToken).ConfigureAwait(false);
                    var runningInstance = await startable.Start(cancellationToken).ConfigureAwait(false);

                    endpoint.Accessor.Set(runningInstance);
                    startedEndpoints.Add(new IMultiEndpointInstance.EndpointInstanceInfo(endpoint.EndpointName, endpoint.ServiceKey, runningInstance));
                }
            }
            catch
            {
                await StopEndpoints(startedEndpoints, CancellationToken.None).ConfigureAwait(false);
                throw;
            }

            return new MultiEndpointInstance(startedEndpoints);
        }

        static async Task StopEndpoints(List<IMultiEndpointInstance.EndpointInstanceInfo> endpoints, CancellationToken cancellationToken)
        {
            for (var index = endpoints.Count - 1; index >= 0; index--)
            {
                try
                {
                    await endpoints[index].Instance.Stop(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Best effort stop when recovering from startup failures.
                }
            }
        }

        readonly IReadOnlyCollection<EndpointHost> endpointHosts;
    }

    sealed class MultiEndpointInstance : IMultiEndpointInstance
    {
        public MultiEndpointInstance(IReadOnlyList<IMultiEndpointInstance.EndpointInstanceInfo> endpoints)
        {
            endpointList = endpoints;
            foreach (var endpoint in endpoints)
            {
                endpointsByName.Add(endpoint.EndpointName, endpoint);
                endpointsByServiceKey.Add(endpoint.ServiceKey, endpoint);
            }
        }

        public IReadOnlyCollection<IMultiEndpointInstance.EndpointInstanceInfo> Endpoints => endpointList;

        public IEndpointInstance this[string endpointName]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(endpointName);
                return endpointsByName[endpointName].Instance;
            }
        }

        public IEndpointInstance this[object serviceKey]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(serviceKey);
                return endpointsByServiceKey[serviceKey].Instance;
            }
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref stopped, 1) != 0)
            {
                return;
            }

            List<Exception>? exceptions = null;

            for (var index = endpointList.Count - 1; index >= 0; index--)
            {
                var endpoint = endpointList[index];

                try
                {
                    await endpoint.Instance.Stop(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException("One or more endpoints failed to stop.", exceptions);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Stop().ConfigureAwait(false);
        }

        readonly Dictionary<string, IMultiEndpointInstance.EndpointInstanceInfo> endpointsByName = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<object, IMultiEndpointInstance.EndpointInstanceInfo> endpointsByServiceKey = [];
        readonly IReadOnlyList<IMultiEndpointInstance.EndpointInstanceInfo> endpointList;
        int stopped;
    }

    sealed class OwnedServiceProviderMultiEndpointInstance : IMultiEndpointInstance
    {
        public OwnedServiceProviderMultiEndpointInstance(IMultiEndpointInstance inner, IServiceProvider serviceProvider)
        {
            this.inner = inner;
            this.serviceProvider = serviceProvider;
        }

        public IReadOnlyCollection<IMultiEndpointInstance.EndpointInstanceInfo> Endpoints => inner.Endpoints;

        public IEndpointInstance this[string endpointName] => inner[endpointName];

        public IEndpointInstance this[object serviceKey] => inner[serviceKey];

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            Exception? stopException = null;

            try
            {
                await inner.Stop(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                stopException = ex;
            }

            await DisposeServiceProviderAsync().ConfigureAwait(false);

            if (stopException != null)
            {
                throw stopException;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Stop().ConfigureAwait(false);
        }

        async Task DisposeServiceProviderAsync()
        {
            if (serviceProvider is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        readonly IMultiEndpointInstance inner;
        readonly IServiceProvider serviceProvider;
        int disposed;
    }
}
