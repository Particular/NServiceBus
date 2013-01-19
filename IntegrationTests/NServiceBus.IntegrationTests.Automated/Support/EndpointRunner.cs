namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using Installation.Environments;

    using NServiceBus.ObjectBuilder;

    [Serializable]
    public class EndpointRunner : MarshalByRefObject
    {
        IStartableBus startableBus;
        IBus bus;
        Configure config;

        EndpointBehavior behavior;
        BehaviorContext behaviorContext;


        public bool Initialize(string assemblyQualifiedName, BehaviorContext context, string transport)
        {
            behaviorContext = context;
            this.behavior = ((BehaviorFactory)Activator.CreateInstance(Type.GetType(assemblyQualifiedName))).Get();
            
            config = Configure.With()
                .DefineEndpointName(this.behavior.EndpointName)
                .CustomConfigurationSource(new ScenarioConfigSource(this.behavior));

            this.behavior.Setups.ForEach(setup=> setup(config));

            config.Configurer.RegisterSingleton(context.GetType(), context);

            ConfigureTransport(transport);
            
            startableBus = config.CreateBus();

            Configure.Instance.ForInstallationOn<Windows>().Install();


            return true;
        }

        void ConfigureTransport(string transport)
        {
            if (string.IsNullOrEmpty(transport))
                return;


            var transportType = Type.GetType(transport);

            if(DefaultConnectionStrings.ContainsKey(transportType))
                config.UseTransport(transportType, DefaultConnectionStrings[transportType]);
            else
                config.UseTransport(transportType);
        }


        public bool Start()
        {
            bus = startableBus.Start();

            this.behavior.Givens.ForEach(a=>a(bus));

            return true;

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

        static Dictionary<Type, string> DefaultConnectionStrings = new Dictionary<Type, string>
            {
                { typeof(RabbitMQ), "host=localhost" },
                { typeof(SqlServer), @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;" },
                { typeof(ActiveMQ),  @"activemq:tcp://localhost:61616" },
               
            };
    }
}