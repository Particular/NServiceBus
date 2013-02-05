namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Installation.Environments;

    [Serializable]
    public class EndpointRunner : MarshalByRefObject
    {
        IBus bus;
        Configure config;

        EndpointBehavior behavior;
        BehaviorContext behaviorContext;
        RunDescriptor runDescriptor;

        public Result Initialize(RunDescriptor descriptor, Type endpointBuilderType, IDictionary<Type, string> routingTable, string endpointName, BehaviorContext context)
        {
            try
            {
                runDescriptor = descriptor;
                behaviorContext = context;
                behavior = ((IEndpointBehaviorFactory)Activator.CreateInstance(endpointBuilderType)).Get();
                behavior.EndpointName = endpointName;

                config = behavior.GetConfiguration(descriptor, routingTable);

                if (behaviorContext != null)
                    config.Configurer.RegisterSingleton(behaviorContext.GetType(), behaviorContext);

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
                ((IDisposable)bus).Dispose();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }  
        }

        public void ApplyWhens()
        {
            behavior.Whens.ForEach(a => a(bus, behaviorContext));
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