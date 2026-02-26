namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
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
        ConfigureHowToCreateInstance((services, config) =>
        {
            var settings = config.GetSettings();
            var runDescriptor = settings.Get<RunDescriptor>();
            string serviceKey = settings.Get<string>("NServiceBus.AcceptanceTesting.EndpointRunnerName");
            runDescriptor.Services.AddNServiceBusEndpoint(config, serviceKey);
            return Task.FromResult(new StartableEndpointInstance(serviceKey));
        }, static (startableEndpoint, provider, cancellationToken) => startableEndpoint.Start(provider, cancellationToken));
    }

    class StartableEndpointInstance : IStartableEndpointWithExternallyManagedContainer
    {
        readonly string serviceKey;
        IEndpointInstance? endpointInstance;

        public StartableEndpointInstance(string serviceKey)
        {
            this.serviceKey = serviceKey;
            MessageSession = new Lazy<IMessageSession>(() => endpointInstance ?? throw new InvalidOperationException("Endpoint instance has not been started yet. MessageSession cannot be accessed before the endpoint is started."));
        }

        public async Task<IEndpointInstance> Start(IServiceProvider builder, CancellationToken cancellationToken = default)
        {
            var starter = builder.GetRequiredKeyedService<IEndpointStarter>(serviceKey);
            endpointInstance = await starter.GetOrStart(cancellationToken).ConfigureAwait(false);
            return endpointInstance;
        }

        public Lazy<IMessageSession> MessageSession { get; }
    }

    [MemberNotNull(nameof(createInstanceCallback), nameof(startInstanceCallback))]
    public void ConfigureHowToCreateInstance<T>(Func<IServiceCollection, EndpointConfiguration, Task<T>> createCallback, Func<T, IServiceProvider, CancellationToken, Task<IEndpointInstance>> startCallback)
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