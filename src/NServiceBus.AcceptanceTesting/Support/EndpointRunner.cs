namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Logging;
    using NServiceBus.Support;
    using Transport;

    public class EndpointRunner : ComponentRunner
    {
        static ILog Logger = LogManager.GetLogger<EndpointRunner>();
        bool doNotFailOnErrorMessages;
        EndpointBehavior behavior;
        IStartableEndpoint startable;
        IEndpointInstance endpointInstance;
        EndpointCustomizationConfiguration configuration;
        ScenarioContext scenarioContext;
        EndpointConfiguration endpointConfiguration;

        public EndpointRunner(bool doNotFailOnErrorMessages)
        {
            this.doNotFailOnErrorMessages = doNotFailOnErrorMessages;
        }

        public async Task Initialize(RunDescriptor run, EndpointBehavior endpointBehavior, string endpointName)
        {
            try
            {
                behavior = endpointBehavior;
                scenarioContext = run.ScenarioContext;
                var endpointConfigurationFactory = (IEndpointConfigurationFactory)Activator.CreateInstance(endpointBehavior.EndpointBuilderType);
                endpointConfigurationFactory.ScenarioContext = run.ScenarioContext;
                configuration = endpointConfigurationFactory.Get();
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
                endpointConfiguration = await configuration.GetConfiguration(run).ConfigureAwait(false);
                RegisterInheritanceHierarchyOfContextInSettings(scenarioContext);

                endpointBehavior.CustomConfig.ForEach(customAction => customAction(endpointConfiguration, scenarioContext));

                startable = await Endpoint.Create(endpointConfiguration).ConfigureAwait(false);

                var transportInfrastructure = endpointConfiguration.GetSettings().Get<TransportInfrastructure>();
                scenarioContext.HasNativePubSubSupport = transportInfrastructure.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize endpoint " + endpointName, ex);
                throw;
            }
        }

        void RegisterInheritanceHierarchyOfContextInSettings(ScenarioContext context)
        {
            var type = context.GetType();
            while (type != typeof(object))
            {
                endpointConfiguration.GetSettings().Set(type.FullName, scenarioContext);
                type = type.BaseType;
            }
        }

        public override async Task Start(CancellationToken token)
        {
            try
            {
                ScenarioContext.CurrentEndpoint = configuration.EndpointName;
                endpointInstance = await startable.Start().ConfigureAwait(false);

                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Endpoint start was aborted");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start endpoint " + configuration.EndpointName, ex);

                throw;
            }
        }

        public override async Task ComponentsStarted(CancellationToken token)
        {
            try
            {
                if (behavior.Whens.Count != 0)
                {
                    await Task.Run(async () =>
                    {
                        var executedWhens = new HashSet<Guid>();

                        while (!token.IsCancellationRequested)
                        {
                            if (executedWhens.Count == behavior.Whens.Count)
                            {
                                break;
                            }

                            if (token.IsCancellationRequested)
                            {
                                break;
                            }

                            foreach (var when in behavior.Whens)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    break;
                                }

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
                    }, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to execute Whens on endpoint{configuration.EndpointName}", ex);

                throw;
            }
        }

        public override async Task Stop()
        {
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

        public override string Name
        {
            get { return configuration.EndpointName; }
        }
    }
}