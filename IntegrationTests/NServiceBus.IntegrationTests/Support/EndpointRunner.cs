namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Generic;
    using Installation.Environments;

    [Serializable]
    public class EndpointRunner : MarshalByRefObject
    {
        IStartableBus startableBus;
        IBus bus;
        Configure config;

        EndpointBehavior behavior;
        BehaviorContext behaviorContext;

        public Result Initialize(RunDescriptor runDescriptor, Type endpointBuilderType, IDictionary<Type, string> routingTable, string endpointName, BehaviorContext context)
        {
            try
            {

                behaviorContext = context;
                behavior = ((IEndpointBehaviorFactory)Activator.CreateInstance(endpointBuilderType)).Get();
                behavior.EndpointName = endpointName;

                config = behavior.GetConfiguration(runDescriptor, routingTable);

                if (behaviorContext != null)
                    config.Configurer.RegisterSingleton(behaviorContext.GetType(), behaviorContext);

                startableBus = config.CreateBus();

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
                bus = startableBus.Start();

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
                bus.Shutdown();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }  
        }

        public void ApplyWhens()
        {
            this.behavior.Whens.ForEach(a => a(bus, behaviorContext));
        }

        public string Name()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

        public bool Done()
        {
            var isDone = behavior.Done(behaviorContext);

            if (isDone)
                Console.Out.WriteLine("Endpoint is done");

            return isDone;
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