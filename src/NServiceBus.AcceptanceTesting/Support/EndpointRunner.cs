namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Remoting.Lifetime;
    using System.Threading;
    using System.Threading.Tasks;
    using Installation.Environments;
    using Logging;
    using NServiceBus.Support;

    [Serializable]
    public class EndpointRunner : MarshalByRefObject
    {
        static ILog Logger = LogManager.GetLogger<EndpointRunner>();
        readonly SemaphoreSlim contextChanged = new SemaphoreSlim(0);
        readonly IList<Guid> executedWhens = new List<Guid>();
        EndpointBehavior behavior;
        IStartableBus bus;
        Configure config;
        EndpointConfiguration configuration;
        Task executeWhens;
        ScenarioContext scenarioContext;
        bool stopped;

        public Result Initialize(RunDescriptor run, EndpointBehavior endpointBehavior,
            IDictionary<Type, string> routingTable, string endpointName)
        {
            try
            {
                behavior = endpointBehavior;
                scenarioContext = run.ScenarioContext;
                configuration =
                    ((IEndpointConfigurationFactory) Activator.CreateInstance(endpointBehavior.EndpointBuilderType))
                        .Get();
                configuration.EndpointName = endpointName;

                if (!string.IsNullOrEmpty(configuration.CustomMachineName))
                {
                    RuntimeEnvironment.MachineNameAction = () => configuration.CustomMachineName;
                }

                //apply custom config settings
                config = configuration.GetConfiguration(run, routingTable);
                endpointBehavior.CustomConfig.ForEach(customAction => customAction(config));
               
                if (scenarioContext != null)
                {
                    config.Configurer.RegisterSingleton(scenarioContext.GetType(), scenarioContext);
                    scenarioContext.ContextPropertyChanged += scenarioContext_ContextPropertyChanged;
                }

                bus = config.CreateBus();

                config.ForInstallationOn<Windows>().Install();

                executeWhens = Task.Factory.StartNew(() =>
                {
                    while (!stopped)
                    {
                        if (!contextChanged.Wait(TimeSpan.FromSeconds(5)))
                            //we spin around each 5s since the callback mechanism seems to be shaky
                        {
                            continue;
                        }

                        lock (behavior)
                        {
                            foreach (var when in behavior.Whens)
                            {
                                if (executedWhens.Contains(when.Id))
                                {
                                    continue;
                                }

                                if (when.ExecuteAction(scenarioContext, bus))
                                {
                                    executedWhens.Add(when.Id);
                                }
                            }
                        }
                    }
                });

                return Result.Success();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize endpoint " + endpointName, ex);
                return Result.Failure(ex);
            }
        }

        private void scenarioContext_ContextPropertyChanged(object sender, EventArgs e)
        {
            contextChanged.Release();
        }

        public Result Start()
        {
            try
            {
                foreach (var given in behavior.Givens)
                {
                    var action = given.GetAction(scenarioContext);

                    action(bus);
                }

                bus.Start();


                return Result.Success();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start endpoint " + configuration.EndpointName, ex);

                return Result.Failure(ex);
            }
        }

        public Result Stop()
        {
            try
            {
                stopped = true;

                scenarioContext.ContextPropertyChanged -= scenarioContext_ContextPropertyChanged;

                executeWhens.Wait();
                contextChanged.Dispose();

                bus.Dispose();

                return Result.Success();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to stop endpoint " + configuration.EndpointName, ex);

                return Result.Failure(ex);
            }
        }

        public override object InitializeLifetimeService()
        {
            var lease = (ILease) base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(2);
                lease.SponsorshipTimeout = TimeSpan.FromMinutes(2);
                lease.RenewOnCallTime = TimeSpan.FromSeconds(2);
            }
            return lease;
        }

        public string Name()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

        [Serializable]
        public class Result : MarshalByRefObject
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