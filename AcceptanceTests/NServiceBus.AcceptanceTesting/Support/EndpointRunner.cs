namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Installation.Environments;

    [Serializable]
    public class EndpointRunner : MarshalByRefObject
    {
        IStartableBus bus;
        Configure config;

        EndpointBehavior behavior;
        ScenarioContext scenarioContext;

        public Result Initialize(RunDescriptor descriptor, Type endpointBuilderType, IDictionary<Type, string> routingTable, string endpointName)
        {
            try
            {
                scenarioContext = descriptor.ScenarioContext;
                behavior = ((IEndpointBehaviorFactory)Activator.CreateInstance(endpointBuilderType)).Get();
                behavior.EndpointName = endpointName;

                config = behavior.GetConfiguration(descriptor, routingTable);

                if (scenarioContext != null)
                    config.Configurer.RegisterSingleton(scenarioContext.GetType(), scenarioContext);

                bus = config.CreateBus();

                Configure.Instance.ForInstallationOn<Windows>().Install();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }
        }

        public Result Start()
        {
            try
            {
                bus.Start();

                behavior.Givens.ForEach(a => a(bus));

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
                bus.Dispose();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }  
        }

        public void ApplyWhens()
        {
            behavior.Whens.ForEach(a => a(bus, scenarioContext));
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