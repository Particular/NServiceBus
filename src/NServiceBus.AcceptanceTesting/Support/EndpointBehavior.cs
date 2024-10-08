﻿namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Customization;
using NUnit.Framework;

public class EndpointBehavior : IComponentBehavior
{
    public EndpointBehavior(IEndpointConfigurationFactory endpointBuilder)
    {
        EndpointBuilder = endpointBuilder;
        CustomConfig = [];
        ConfigureHowToCreateInstance(config => Endpoint.Create(config), static (startableEndpoint, cancellationToken) => startableEndpoint.Start(cancellationToken));
    }

    public void ConfigureHowToCreateInstance<T>(Func<EndpointConfiguration, Task<T>> createCallback, Func<T, CancellationToken, Task<IEndpointInstance>> startCallback)
    {
        createInstanceCallback = async config =>
        {
            var result = await createCallback(config).ConfigureAwait(false);
            return result;
        };
        startInstanceCallback = (state, ct) => startCallback((T)state, ct);
    }

    public IEndpointConfigurationFactory EndpointBuilder { get; }

    public List<IWhenDefinition> Whens { get; set; }

    public List<Action<EndpointConfiguration, ScenarioContext>> CustomConfig { get; }

    public bool DoNotFailOnErrorMessages { get; set; }

    public async Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var endpointName = Conventions.EndpointNamingConvention(EndpointBuilder.GetType());

        var runner = new EndpointRunner(createInstanceCallback, startInstanceCallback, DoNotFailOnErrorMessages);

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

    Func<EndpointConfiguration, Task<object>> createInstanceCallback;
    Func<object, CancellationToken, Task<IEndpointInstance>> startInstanceCallback;
}