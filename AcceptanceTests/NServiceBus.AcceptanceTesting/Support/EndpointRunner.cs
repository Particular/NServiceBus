namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Remoting.Lifetime;
    using System.Threading;
    using System.Threading.Tasks;
    using Installation.Environments;
    using Logging;

    [Serializable]
    public class EndpointRunner : MarshalByRefObject
    {
        IStartableBus bus;
        Configure config;

        EndpointConfiguration configuration;
        ScenarioContext scenarioContext;
        EndpointBehaviour behaviour;
        Semaphore contextChanged = new Semaphore(0, int.MaxValue);
        bool stopped = false;

        public Result Initialize(RunDescriptor run, EndpointBehaviour endpointBehaviour, IDictionary<Type, string> routingTable, string endpointName)
        {
            try
            {
                behaviour = endpointBehaviour;
                scenarioContext = run.ScenarioContext;
                configuration = ((IEndpointConfigurationFactory)Activator.CreateInstance(endpointBehaviour.EndpointBuilderType)).Get();
                configuration.EndpointName = endpointName;

                if (!string.IsNullOrEmpty(configuration.CustomMachineName))
                {
                    NServiceBus.Support.RuntimeEnvironment.MachineNameAction = () => configuration.CustomMachineName;
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

                Task.Factory.StartNew(() =>
                    {
                        while (!stopped)
                        {
                            contextChanged.WaitOne(TimeSpan.FromSeconds(5)); //we spin around each 5 s since the callback mechanism seems to be shaky

                            foreach (var when in behaviour.Whens)
                            {
                                when.ExecuteAction(scenarioContext, bus);
                            }
                        }
                    });

                return Result.Success();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initalize endpoint " + endpointName,ex);
                return Result.Failure(ex);
            }
        }

        void scenarioContext_ContextPropertyChanged(object sender, EventArgs e)
        {
            contextChanged.Release();
        }


        public Result Start()
        {
            try
            {
                foreach (var given in behaviour.Givens)
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
            ILease lease = (ILease)base.InitializeLifetimeService();
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
        static readonly ILog Logger = LogManager.GetLogger(typeof(EndpointRunner));

        [Serializable]
        public class Result : MarshalByRefObject
        {
            public string ExceptionMessage { get; set; }

            public bool Failed
            {
                get { return ExceptionMessage != null; }

            }

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

            public Type ExceptionType { get; set; }
        }
    }


}