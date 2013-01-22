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

        public bool Initialize(RunDescriptor runDescriptor, BehaviorDescriptor behaviorDescriptor, IDictionary<Type, string> routingTable)
        {

            behaviorContext = behaviorDescriptor.Context;
            behavior = ((IEndpointBehaviorFactory)Activator.CreateInstance(behaviorDescriptor.EndpointBuilderType)).Get();
            behavior.EndpointName = behaviorDescriptor.EndpointName + "." + runDescriptor.Key + "." + runDescriptor.Permutation;

            config = behavior.GetConfiguration(runDescriptor, routingTable);

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
            this.behavior.Whens.ForEach(a => a(bus));
        }

        public string Name()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

        public bool Done()
        {
            return this.behavior.Done(behaviorContext);
        }
    }
}