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
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EndpointRunner));
        private readonly SemaphoreSlim contextChanged = new SemaphoreSlim(0);
        private readonly IList<Guid> executedWhens = new List<Guid>();
        private EndpointBehaviour behaviour;
        private IStartableBus bus;
        private Configure config;
        private EndpointConfiguration configuration;
        private Task executeWhens;
        private ScenarioContext scenarioContext;
        private bool stopped;

        public Result Initialize(RunDescriptor run, EndpointBehaviour endpointBehaviour,
            IDictionary<Type, string> routingTable, string endpointName)
        {
            try
            {
                behaviour = endpointBehaviour;
                scenarioContext = run.ScenarioContext;
                configuration =
                    ((IEndpointConfigurationFactory) Activator.CreateInstance(endpointBehaviour.EndpointBuilderType))
                        .Get();
                configuration.EndpointName = endpointName;

                if (!string.IsNullOrEmpty(configuration.CustomMachineName))
                {
                    RuntimeEnvironment.MachineNameAction = () => configuration.CustomMachineName;
                }

                //apply custom config settings
                endpointBehaviour.CustomConfig.ForEach(customAction => customAction(config));
                config = configuration.GetConfiguration(run, routingTable);

                if (scenarioContext != null)
                {
                    config.Configurer.RegisterSingleton(scenarioContext.GetType(), scenarioContext);
                    scenarioContext.ContextPropertyChanged += scenarioContext_ContextPropertyChanged;
                }

                bus = config.CreateBus();

                Configure.Instance.ForInstallationOn<Windows>().Install();

                executeWhens = Task.Factory.StartNew(() =>
                {
                    while (!stopped)
                    {
                        if (!contextChanged.Wait(TimeSpan.FromSeconds(5)))
                            //we spin around each 5s since the callback mechanism seems to be shaky
                        {
                            continue;
                        }

                        lock (behaviour)
                        {
                            foreach (IWhenDefinition when in behaviour.Whens)
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
                Logger.Error("Failed to initalize endpoint " + endpointName, ex);
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
                foreach (IGivenDefinition given in behaviour.Givens)
                {
                    Action<IBus> action = given.GetAction(scenarioContext);

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
            public string ExceptionMessage { get; set; }

            public bool Failed
            {
                get { return ExceptionMessage != null; }
            }

            public Type ExceptionType { get; set; }

            public static Result Success()
            {
                return new Result();
            }

            public static Result Failure(Exception ex)
            {
                return new Result
                {
                    ExceptionMessage = ex.ToString(),
                    ExceptionType = ex.GetType()
                };
            }
        }
    }
}