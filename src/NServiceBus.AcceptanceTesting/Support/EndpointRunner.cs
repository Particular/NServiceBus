namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;
    using Logging;
    using NServiceBus.Support;
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

                if (!string.IsNullOrEmpty(configuration.CustomMachineName))
                {
                    RuntimeEnvironment.MachineNameAction = () => configuration.CustomMachineName;
                }

                //apply custom config settings
                if (configuration.GetConfiguration == null)
                {
                    throw new Exception($"Missing EndpointSetup<T> in the constructor of {endpointName} endpoint.");
                }
                var endpointConfiguration = await configuration.GetConfiguration(run).ConfigureAwait(false);
                RegisterInheritanceHierarchyOfContextInSettings(scenarioContext, endpointConfiguration);
                TrackFailingMessages(endpointName, endpointConfiguration);

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
            endpointConfiguration.Recoverability().Failed(settings => settings.OnMessageSentToErrorQueue((m, _) =>
            {
                scenarioContext.FailedMessages.AddOrUpdate(
                    endpointName,
                    new[]
                    {
                        new FailedMessage(m.MessageId, new Dictionary<string, string>(m.Headers), m.Body, m.Exception, m.ErrorQueue)
                    },
                    (i, failed) =>
                    {
                        var result = failed.ToList();
                        result.Add(new FailedMessage(m.MessageId, new Dictionary<string, string>(m.Headers), m.Body, m.Exception, m.ErrorQueue));
                        return result;
                    });

                //We need to set the error flag to false as we want to reset all processing exceptions caused by immediate retries
                scenarioContext.UnfinishedFailedMessages.AddOrUpdate(m.MessageId, id => false, (id, value) => false);

                return Task.FromResult(0);
            }));
            endpointConfiguration.Pipeline.Register(new CaptureExceptionBehavior(scenarioContext.UnfinishedFailedMessages), "Captures unhandled exceptions from processed messages for the AcceptanceTesting Framework");
        }

        void RegisterInheritanceHierarchyOfContextInSettings(ScenarioContext context, EndpointConfiguration endpointConfiguration)
        {
            var type = context.GetType();
            while (type != typeof(object))
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
                    await Task.Run(async () =>
                    {
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
                    }, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Logger.Error($"Failed to execute Whens on endpoint{configuration.EndpointName}", ex);

                throw;
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
