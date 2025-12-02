namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Transport;

public class EndpointRunner(
    Func<IServiceCollection, EndpointConfiguration, Task<object>> createCallback,
    Func<object, IServiceProvider, CancellationToken, Task<IEndpointInstance>> startCallback,
    bool doNotFailOnErrorMessages,
    int instanceIndex)
    : ComponentRunner
{
    static readonly ILog Logger = LogManager.GetLogger<EndpointRunner>();
    EndpointBehavior behavior;
    object startable;
    IEndpointInstance endpointInstance;
    EndpointCustomizationConfiguration configuration;
    ScenarioContext scenarioContext;
    KeyedServiceCollectionAdapter services;
    RunDescriptor runDescriptor;
    IServiceProvider serviceProvider;

    public async Task Initialize(RunDescriptor run, EndpointBehavior endpointBehavior, string endpointName)
    {
        ScenarioContext.CurrentEndpoint = endpointName;
        try
        {
            behavior = endpointBehavior;
            runDescriptor = run;
            scenarioContext = runDescriptor.ScenarioContext;
            endpointBehavior.EndpointBuilder.ScenarioContext = runDescriptor.ScenarioContext;
            configuration = endpointBehavior.EndpointBuilder.Get();
            configuration.EndpointName = endpointName;

            //apply custom config settings
            if (configuration.GetConfiguration == null)
            {
                throw new Exception($"Missing EndpointSetup<T> in the constructor of {endpointName} endpoint.");
            }
            var endpointConfiguration = await configuration.GetConfiguration(runDescriptor).ConfigureAwait(false);
            RegisterScenarioContext(endpointConfiguration);
            TrackFailingMessages(endpointName, endpointConfiguration);

            if (!string.IsNullOrEmpty(configuration.CustomMachineName))
            {
                endpointConfiguration.UniquelyIdentifyRunningInstance().UsingHostName(configuration.CustomMachineName);
            }

            endpointConfiguration.EnableFeature<FeatureStartupTaskRunner>();

            endpointBehavior.CustomConfig.ForEach(customAction => customAction(endpointConfiguration, scenarioContext));

            services = new KeyedServiceCollectionAdapter(runDescriptor.Services, Name);

            endpointBehavior.ServicesBeforeStart.ForEach(customAction => customAction(services, scenarioContext));

            startable = await createCallback(services, endpointConfiguration).ConfigureAwait(false);

            var transportDefinition = endpointConfiguration.GetSettings().Get<TransportDefinition>();
            scenarioContext.HasNativePubSubSupport = transportDefinition.SupportsPublishSubscribe;

            endpointBehavior.ServicesAfterStart.ForEach(customAction => customAction(services, scenarioContext));
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to initialize endpoint " + endpointName, ex);
            throw;
        }
    }

    void TrackFailingMessages(string endpointName, EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.Pipeline.Register(new CaptureExceptionBehavior(scenarioContext.UnfinishedFailedMessages), "Captures unhandled exceptions from processed messages for the AcceptanceTesting Framework");
        endpointConfiguration.Pipeline.Register(new CaptureRecoverabilityActionBehavior(endpointName, scenarioContext), "Marks failed and discarded messages for the AcceptanceTesting Framework");
    }

    void RegisterScenarioContext(EndpointConfiguration endpointConfiguration)
    {
        var type = scenarioContext.GetType();
        while (type != typeof(object) && type is not null)
        {
            endpointConfiguration.GetSettings().Set(type.FullName, scenarioContext);
            type = type.BaseType;
        }
    }

    public override async Task Start(CancellationToken cancellationToken = default)
    {
        ScenarioContext.CurrentEndpoint = configuration.EndpointName;
        try
        {
            serviceProvider = new KeyedServiceProviderAdapter(runDescriptor.ServiceProvider!, Name, services);
            endpointInstance = await startCallback(startable, serviceProvider, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            Logger.Error("Failed to start endpoint " + Name, ex);

            throw;
        }
    }

    public override async Task ComponentsStarted(CancellationToken cancellationToken = default)
    {
        ScenarioContext.CurrentEndpoint = configuration.EndpointName;
        try
        {
            if (behavior.Whens.Count != 0)
            {
                await ExecuteWhens(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            Logger.Error($"Failed to execute Whens on endpoint{Name}", ex);

            throw;
        }
    }

    async Task ExecuteWhens(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        var executedWhens = new HashSet<Guid>();

        while (true)
        {
            if (executedWhens.Count == behavior.Whens.Count)
            {
                break;
            }

            foreach (var when in behavior.Whens)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (executedWhens.Contains(when.Id))
                {
                    continue;
                }

                if (await when.ExecuteAction(scenarioContext, endpointInstance).ConfigureAwait(false))
                {
                    executedWhens.Add(when.Id);
                }
            }

            await Task.Yield(); // enforce yield current context, tight loop could introduce starvation
        }
    }

    public override async Task Stop(CancellationToken cancellationToken = default)
    {
        ScenarioContext.CurrentEndpoint = configuration.EndpointName;
        try
        {
            if (endpointInstance != null)
            {
                await endpointInstance.Stop(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            Logger.Error("Failed to stop endpoint " + configuration.EndpointName, ex);
            throw;
        }
        finally
        {
            if (serviceProvider is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
        }

        if (!doNotFailOnErrorMessages)
        {
            ThrowOnFailedMessages();
        }
    }

    void ThrowOnFailedMessages()
    {
        foreach (var failedMessage in scenarioContext.FailedMessages.Where(kvp => kvp.Key == configuration.EndpointName))
        {
            throw new MessageFailedException(failedMessage.Value.First(), scenarioContext);
        }
    }

    public override string Name
    {
        get => $"{configuration.EndpointName}_{field}";
    } = instanceIndex.ToString(CultureInfo.InvariantCulture);
}