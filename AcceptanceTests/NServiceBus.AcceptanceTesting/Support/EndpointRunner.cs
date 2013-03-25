using System.Security.Permissions;

namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Remoting.Lifetime;
    using System.Threading.Tasks;
    using NServiceBus.Installation.Environments;

    [Serializable]
    public class EndpointRunner : MarshalByRefObject
    {
        IStartableBus bus;
        Configure config;

        EndpointConfiguration configuration;
        ScenarioContext scenarioContext;
        EndpointBehaviour behaviour;
        TimeSpan testExecutionTimeout;

        public Result Initialize(RunDescriptor run, EndpointBehaviour endpointBehaviour, IDictionary<Type, string> routingTable, string endpointName)
        {
            try
            {
                behaviour = endpointBehaviour;
                scenarioContext = run.ScenarioContext;
                testExecutionTimeout = run.TestExecutionTimeout;
                configuration = ((IEndpointConfigurationFactory)Activator.CreateInstance(endpointBehaviour.EndpointBuilderType)).Get();
                configuration.EndpointName = endpointName;

                config = configuration.GetConfiguration(run, routingTable);

                //apply custom config settings
                endpointBehaviour.CustomConfig.ForEach(customAction => customAction(config));


                if (scenarioContext != null)
                {
                    config.Configurer.RegisterSingleton(scenarioContext.GetType(), scenarioContext);
                    scenarioContext.ContextPropertyChanged += scenarioContext_ContextPropertyChanged;
                }


                bus = config.CreateBus();

                Configure.Instance.ForInstallationOn<Windows>().Install();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }
        }

        void scenarioContext_ContextPropertyChanged(object sender, EventArgs e)
        {

            //HACK: kick off another thread so that we'll read the context after the actual value has changed
            // we need to find a better way
            Task.Factory.StartNew(() =>
                {
                    foreach (var when in behaviour.Whens)
                    {
                        var action = when.GetAction(scenarioContext);
                        action(bus);
                    }                    
                });
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
                return Result.Failure(ex);
            }
        }

        public Result Stop()
        {
            try
            {
                scenarioContext.ContextPropertyChanged -= scenarioContext_ContextPropertyChanged;
                bus.Dispose();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null;
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