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

        public bool Initialize(RunDescriptor runDescriptor, Type endpointBuilderType, IDictionary<Type, string> routingTable, string endpointName, BehaviorContext context)
        {

            behaviorContext = context;
            behavior = ((IEndpointBehaviorFactory)Activator.CreateInstance(endpointBuilderType)).Get();
            behavior.EndpointName = endpointName;

            config = behavior.GetConfiguration(runDescriptor, routingTable);

            if (behaviorContext!= null)
                config.Configurer.RegisterSingleton(behaviorContext.GetType(), behaviorContext);

            startableBus = config.CreateBus();

            Configure.Instance.ForInstallationOn<Windows>().Install();

            return true;
        }

        public bool Start()
        {
            bus = startableBus.Start();

            this.behavior.Givens.ForEach(a => a(bus));

            return true;
        }

        public void Stop()
        {
            bus.Shutdown();
        }

        public void ApplyWhens()
        {
            this.behavior.Whens.ForEach(a => a(bus,behaviorContext));
        }

        public string Name()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

        public bool Done()
        {
            var isDone =behavior.Done(behaviorContext);

            if(isDone)
                Console.Out.WriteLine("Endpoint is done");

            return isDone;
        }
    }
}