namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using Installation.Environments;

    [Serializable]
    public class EndpointRunner:MarshalByRefObject
    {
        IStartableBus startableBus;
        IBus bus;
        Configure config;

        EndpointBehaviour behaviour;


        public bool Initialize(string assemblyQualifiedName, string transport)
        {
            behaviour = ((BehaviourFactory)Activator.CreateInstance(Type.GetType(assemblyQualifiedName))).Get();



            config = Configure.With()
                .DefineEndpointName(behaviour.EndpointName)
                .CustomConfigurationSource(new ScenarioConfigSource(behaviour));

            behaviour.SetupActions.ForEach(setup=> setup(config));

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

            behaviour.Givens.ForEach(a=>a(bus));

            return true;

        }


        public void ApplyWhens()
        {
            behaviour.Whens.ForEach(a => a(bus));
        }

        public string Name()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

      
        public bool Done()
        {
            return behaviour.Done();
        }

        public string[] VerifyAssertions()
        {
            var failures = new List<string>();
            foreach (var assertion in behaviour.Assertions)
            {
                var failure = "";
                try
                {
                    assertion();
                }
                catch (Exception ex)
                {
                    failure = ex.Message;
                }

                if(!string.IsNullOrEmpty(failure))
                    failures.Add(failure);
            }

            return failures.ToArray();
        }

        static Dictionary<Type, string> DefaultConnectionStrings = new Dictionary<Type, string>
            {
                { typeof(RabbitMQ), "host=localhost" },
                { typeof(SqlServer), @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;" },
                { typeof(ActiveMQ),  @"activemq:tcp://localhost:61616" },
               
            };
    }
}