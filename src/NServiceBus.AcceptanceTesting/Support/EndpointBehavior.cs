namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Customization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class EndpointBehavior : IComponentBehavior
{
    readonly int instanceIndex;

    public EndpointBehavior(IEndpointConfigurationFactory endpointBuilder, int instanceIndex)
    {
        this.instanceIndex = instanceIndex;
        EndpointBuilder = endpointBuilder;
        Whens = [];
        CustomConfig = [];
        ServicesBeforeStart = [];
        ServicesAfterStart = [];
        ResolvesBeforeStart = [];
        ResolveAfterStart = [];
        ConfigureHowToCreateInstance((services, config) =>
        {
            if (services is not KeyedServiceCollectionAdapter collectionAdapter)
            {
                throw new InvalidOperationException(
                    $"The default endpoint creation callback requires a {nameof(KeyedServiceCollectionAdapter)} " +
                    $"but received {services.GetType().Name}. Call {nameof(ConfigureHowToCreateInstance)} to " +
                    "provide a custom creation strategy.");
            }

            var serviceKey = collectionAdapter.ServiceKey;

            collectionAdapter.Inner.AddNServiceBusEndpoint(config, serviceKey);

            return Task.FromResult(new StartableEndpointInstance(serviceKey));
        }, static (startableEndpoint, provider, cancellationToken) => startableEndpoint.Start(provider, cancellationToken));
    }

    class StartableEndpointInstance(object serviceKey)
    {
        public async Task<IStoppableEndpointInstance> Start(IServiceProvider builder, CancellationToken cancellationToken = default)
        {
            var endpointLifecycle = builder.GetRequiredKeyedService<IEndpointLifecycle>(serviceKey);
            await endpointLifecycle.CreateAndStart(cancellationToken).ConfigureAwait(false);
            var messageSession = builder.GetRequiredKeyedService<IMessageSession>(serviceKey);
            return new StoppableLifecycleEndpoint(endpointLifecycle, messageSession);
        }
    }

    class StoppableLifecycleEndpoint(IEndpointLifecycle endpointLifecycle, IMessageSession messageSession) : IStoppableEndpointInstance
    {
        public IMessageSession MessageSession => messageSession;
        public async Task Stop(CancellationToken cancellationToken = default) => await endpointLifecycle.Stop(cancellationToken).ConfigureAwait(false);
    }

    [MemberNotNull(nameof(createInstanceCallback), nameof(startInstanceCallback))]
    public void ConfigureHowToCreateInstance<T>(Func<IServiceCollection, EndpointConfiguration, Task<T>> createCallback, Func<T, IServiceProvider, CancellationToken, Task<IStoppableEndpointInstance>> startCallback)
        where T : notnull
    {
        createInstanceCallback = async (services, config) =>
        {
            var result = await createCallback(services, config).ConfigureAwait(false);
            return result;
        };
        startInstanceCallback = (state, provider, ct) => startCallback((T)state, provider, ct);
    }

    public IEndpointConfigurationFactory EndpointBuilder { get; }

    public List<IWhenDefinition> Whens { get; }

    public List<Action<EndpointConfiguration, ScenarioContext>> CustomConfig { get; }

    public List<Action<IServiceCollection, ScenarioContext>> ServicesBeforeStart { get; }

    public List<Action<IServiceCollection, ScenarioContext>> ServicesAfterStart { get; }

    public List<Func<IServiceProvider, ScenarioContext, CancellationToken, Task>> ResolvesBeforeStart { get; }

    public List<Func<IServiceProvider, ScenarioContext, CancellationToken, Task>> ResolveAfterStart { get; }

    public bool DoNotFailOnErrorMessages { get; set; }

    public async Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var endpointName = Conventions.EndpointNamingConvention(EndpointBuilder.GetType());

        var runner = new EndpointRunner(createInstanceCallback, startInstanceCallback, DoNotFailOnErrorMessages, instanceIndex);

        try
        {
            await runner.Initialize(run, this, endpointName).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await TestContext.Out.WriteLineAsync($"Endpoint {runner.Name} failed to initialize").ConfigureAwait(false);
            throw;
        }
        return runner;
    }

    Func<IServiceCollection, EndpointConfiguration, Task<object>> createInstanceCallback;
    Func<object, IServiceProvider, CancellationToken, Task<IStoppableEndpointInstance>> startInstanceCallback;
}