namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Logging;
    using NServiceBus.Support;
    using NServiceBus.Transports;

    public class EndpointRunner
    {
        static ILog Logger = LogManager.GetLogger<EndpointRunner>();
        CancellationTokenSource stopSource = new CancellationTokenSource();
        EndpointBehavior behavior;
        IStartableBus bus;
        ISendOnlyBus sendOnlyBus;
        EndpointConfiguration configuration;
        ScenarioContext scenarioContext;
        BusConfiguration busConfiguration;
        CancellationToken stopToken;

        public Result Initialize(RunDescriptor run, EndpointBehavior endpointBehavior,
            IDictionary<Type, string> routingTable, string endpointName)
        {
            try
            {
                behavior = endpointBehavior;
                scenarioContext = run.ScenarioContext;
                configuration =
                    ((IEndpointConfigurationFactory)Activator.CreateInstance(endpointBehavior.EndpointBuilderType))
                        .Get();
                configuration.EndpointName = endpointName;

                if (!string.IsNullOrEmpty(configuration.CustomMachineName))
                {
                    RuntimeEnvironment.MachineNameAction = () => configuration.CustomMachineName;
                }

                //apply custom config settings
                busConfiguration = configuration.GetConfiguration(run, routingTable);
                busConfiguration.GetSettings().Set<ScenarioContext>(scenarioContext);

                endpointBehavior.CustomConfig.ForEach(customAction => customAction(busConfiguration));

                if (configuration.SendOnly)
                {
                    sendOnlyBus = Bus.CreateSendOnly(busConfiguration);
                }
                else
                {
                    bus = Bus.Create(busConfiguration);
                    var transportDefinition = busConfiguration.GetSettings().Get<TransportDefinition>();

                    scenarioContext.HasNativePubSubSupport = transportDefinition.HasNativePubSubSupport;
                }

                stopToken = stopSource.Token;

                return Result.Success();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize endpoint " + endpointName, ex);
                return Result.Failure(ex);
            }
        }

        public async Task<Result> Start()
        {
            try
            {
                foreach (var given in behavior.Givens)
                {
                    var action = given.GetAction(scenarioContext);

                    if (configuration.SendOnly)
                    {
                        await action(new IBusAdapter(sendOnlyBus)).ConfigureAwait(false);
                    }
                    else
                    {
                        await action(bus).ConfigureAwait(false);
                    }
                }

                if (!configuration.SendOnly)
                {
                    bus.Start();
                }

                if (behavior.Whens.Count != 0)
                {
                    await Task.Run(async() =>
                    {
                        var executedWhens = new List<Guid>();

                        while (!stopToken.IsCancellationRequested)
                        {
                            if (executedWhens.Count == behavior.Whens.Count)
                            {
                                break;
                            }

                            if (stopToken.IsCancellationRequested)
                            {
                                break;
                            }

                            foreach (var when in behavior.Whens)
                            {
                                if (executedWhens.Contains(when.Id))
                                {
                                    continue;
                                }

                                if (await when.ExecuteAction(scenarioContext, bus))
                                {
                                    executedWhens.Add(when.Id);
                                }
                            }
                        }
                    }, stopToken);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start endpoint " + configuration.EndpointName, ex);

                return Result.Failure(ex);
            }
        }

        public Task<Result> Stop()
        {
            try
            {
                stopSource.Cancel();

                if (configuration.SendOnly)
                {
                    sendOnlyBus.Dispose();
                }
                else
                {
                    bus.Dispose();
                }

                Cleanup();

                return Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to stop endpoint " + configuration.EndpointName, ex);

                return Task.FromResult(Result.Failure(ex));
            }
        }

        void Cleanup()
        {
            dynamic transportCleaner;
            if (busConfiguration.GetSettings().TryGet("CleanupTransport", out transportCleaner))
            {
                transportCleaner.Cleanup();
            }

            dynamic persistenceCleaner;
            if (busConfiguration.GetSettings().TryGet("CleanupPersistence", out persistenceCleaner))
            {
                persistenceCleaner.Cleanup();
            }
        }

        public string Name()
        {
            return configuration.EndpointName;
        }

        public class Result
        {
            public Exception Exception { get; set; }

            public bool Failed
            {
                get { return Exception != null; }
            }

            public static Result Success()
            {
                return new Result();
            }

            public static Result Failure(Exception ex)
            {
                var baseException = ex.GetBaseException();

                if (ex.GetType().IsSerializable)
                {
                    return new Result
                    {
                        Exception = baseException
                    };
                }

                return new Result
                {
                    Exception = new Exception(baseException.Message)
                };
            }
        }
    }
}