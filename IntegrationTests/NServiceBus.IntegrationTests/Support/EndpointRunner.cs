namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Installation.Environments;

    [Serializable]
    public class EndpointRunner : MarshalByRefObject
    {
        IStartableBus startableBus;
        IBus bus;
        Configure config;

        EndpointBehavior behavior;
        BehaviorContext behaviorContext;

        public bool Initialize(string assemblyQualifiedName, BehaviorContext context, IDictionary<string, string> settings)
        {
            behaviorContext = context;
            this.behavior = ((BehaviorFactory)Activator.CreateInstance(Type.GetType(assemblyQualifiedName))).Get();
            
            config = Configure.With()
                .DefineEndpointName(this.behavior.EndpointName)
                .CustomConfigurationSource(new ScenarioConfigSource(this.behavior));

            this.behavior.Setups.ForEach(setup=> setup(settings, config));

            config.Configurer.RegisterSingleton(context.GetType(), context);

            startableBus = config.CreateBus();

            Configure.Instance.ForInstallationOn<Windows>().Install();
            
            return true;
        }

        public bool Start()
        {
            bus = startableBus.Start();

            this.behavior.Givens.ForEach(a=>a(bus));

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