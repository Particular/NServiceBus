namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
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
        CustomConfig = [];
        ConfigureHowToCreateInstance((services, config) => Task.FromResult(EndpointWithExternallyManagedContainer.Create(config, services)), static (startableEndpoint, provider, cancellationToken) => startableEndpoint.Start(provider, cancellationToken));
    }

    public void ConfigureHowToCreateInstance<T>(Func<IServiceCollection, EndpointConfiguration, Task<T>> createCallback, Func<T, IServiceProvider, CancellationToken, Task<IEndpointInstance>> startCallback)
    {
        createInstanceCallback = async (services, config) =>
        {
            var result = await createCallback(services, config).ConfigureAwait(false);
            return result;
        };
        startInstanceCallback = (state, provider, ct) => startCallback((T)state, provider, ct);
    }

    public IEndpointConfigurationFactory EndpointBuilder { get; }

    public List<IWhenDefinition> Whens { get; set; }

    public List<Action<EndpointConfiguration, ScenarioContext>> CustomConfig { get; }

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
    Func<object, IServiceProvider, CancellationToken, Task<IEndpointInstance>> startInstanceCallback;
}