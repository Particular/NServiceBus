namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Transport;

    public class EndpointRunner : ComponentRunner
    {
        static ILog Logger = LogManager.GetLogger<EndpointRunner>();
        Func<EndpointConfiguration, Task<object>> createCallback;
        Func<object, Task<IEndpointInstance>> startCallback;
        bool doNotFailOnErrorMessages;
        EndpointBehavior behavior;
        object startable;
        IEndpointInstance endpointInstance;
        EndpointCustomizationConfiguration configuration;
        ScenarioContext scenarioContext;

        public EndpointRunner(Func<EndpointConfiguration, Task<object>> createCallback, Func<object, Task<IEndpointInstance>> startCallback, bool doNotFailOnErrorMessages)
        {
            this.createCallback = createCallback;
            this.startCallback = startCallback;
            this.doNotFailOnErrorMessages = doNotFailOnErrorMessages;
        }

        public async Task Initialize(RunDescriptor run, EndpointBehavior endpointBehavior, string endpointName)
        {
            ScenarioContext.CurrentEndpoint = endpointName;
            try
            {
                behavior = endpointBehavior;
                scenarioContext = run.ScenarioContext;
                endpointBehavior.EndpointBuilder.ScenarioContext = run.ScenarioContext;
                configuration = endpointBehavior.EndpointBuilder.Get();
                configuration.EndpointName = endpointName;

                //apply custom config settings
                if (configuration.GetConfiguration == null)
                {
                    throw new Exception($"Missing EndpointSetup<T> in the constructor of {endpointName} endpoint.");
                }
                var endpointConfiguration = await configuration.GetConfiguration(run).ConfigureAwait(false);
                RegisterScenarioContext(endpointConfiguration);
                TrackFailingMessages(endpointName, endpointConfiguration);

                if (!string.IsNullOrEmpty(configuration.CustomMachineName))
                {
                    endpointConfiguration.UniquelyIdentifyRunningInstance().UsingHostName(configuration.CustomMachineName);
                }

                endpointConfiguration.EnableFeature<FeatureStartupTaskRunner>();

                endpointBehavior.CustomConfig.ForEach(customAction => customAction(endpointConfiguration, scenarioContext));

                startable = await createCallback(endpointConfiguration).ConfigureAwait(false);

                var transportDefinition = endpointConfiguration.GetSettings().Get<TransportDefinition>();
                scenarioContext.HasNativePubSubSupport = transportDefinition.SupportsPublishSubscribe;
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
            while (type != typeof(object))
            {
                var currentType = type;
                endpointConfiguration.GetSettings().Set(currentType.FullName, scenarioContext);
                endpointConfiguration.RegisterComponents(serviceCollection => serviceCollection.AddSingleton(currentType, scenarioContext));
                type = type.BaseType;
            }
        }

        public override async Task Start(CancellationToken cancellationToken = default)
        {
            ScenarioContext.CurrentEndpoint = configuration.EndpointName;
            try
            {
                endpointInstance = await startCallback(startable).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start endpoint " + configuration.EndpointName, ex);

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
                Logger.Error($"Failed to execute Whens on endpoint{configuration.EndpointName}", ex);

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

        public override async Task Stop()
        {
            ScenarioContext.CurrentEndpoint = configuration.EndpointName;
            try
            {
                if (endpointInstance != null)
                {
                    await endpointInstance.Stop().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to stop endpoint " + configuration.EndpointName, ex);
                throw;
            }

            if (!doNotFailOnErrorMessages)
            {
                ThrowOnFailedMessages();
            }
        }

        void ThrowOnFailedMessages()
        {
            foreach (var failedMessage in scenarioContext.FailedMessages.Where(kvp => kvp.Key == Name))
            {
                throw new MessageFailedException(failedMessage.Value.First(), scenarioContext);
            }
        }

        public override string Name => configuration.EndpointName;
    }
}
