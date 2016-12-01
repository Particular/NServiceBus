namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvanceExtensibility;
    using Logging;
    using NServiceBus.Support;
    using Transport;

    public class EndpointRunner
    {
        static ILog Logger = LogManager.GetLogger<EndpointRunner>();
        EndpointBehavior behavior;
        IStartableEndpoint startable;
        IEndpointInstance endpointInstance;
        EndpointCustomizationConfiguration configuration;
        ScenarioContext scenarioContext;
        RunSettings runSettings;
        EndpointConfiguration endpointConfiguration;

        public bool FailOnErrorMessage => !behavior.DoNotFailOnErrorMessages;

        public async Task Initialize(RunDescriptor run, EndpointBehavior endpointBehavior,
            IDictionary<Type, string> routingTable, string endpointName)
        {
            try
            {
                behavior = endpointBehavior;
                scenarioContext = run.ScenarioContext;
                runSettings = run.Settings;
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
                endpointConfiguration = await configuration.GetConfiguration(run, routingTable).ConfigureAwait(false);
                RegisterInheritanceHierarchyOfContextInSettings(scenarioContext);

                endpointBehavior.CustomConfig.ForEach(customAction => customAction(endpointConfiguration, scenarioContext));

                if (configuration.SendOnly)
                {
                    endpointConfiguration.SendOnly();
                }

                startable = await Endpoint.Create(endpointConfiguration).ConfigureAwait(false);

                if (!configuration.SendOnly)
                {
                    var transportInfrastructure = endpointConfiguration.GetSettings().Get<TransportInfrastructure>();
                    scenarioContext.HasNativePubSubSupport = transportInfrastructure.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast;
                }
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

        public async Task Start(CancellationToken token)
        {
            try
            {
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

        public async Task Whens(CancellationToken token)
        {
            try
            {
                if (behavior.Whens.Count != 0)
                {
                    await Task.Run(async () =>
                    {
                        var executedWhens = new List<Guid>();

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

        public async Task Stop()
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
            finally
            {
                await Cleanup().ConfigureAwait(false);
            }
        }

        Task Cleanup()
        {
            ActiveTestExecutionConfigurer cleaners;
            var cleanersKey = "ConfigureTestExecution." + configuration.EndpointName;
            if (runSettings.TryGet(cleanersKey, out cleaners))
            {
                var tasks = cleaners.Select(cleaner => cleaner.Cleanup());
                return Task.WhenAll(tasks);
            }

            return Task.FromResult(0);
        }

        public string Name()
        {
            return configuration.EndpointName;
        }
    }
}