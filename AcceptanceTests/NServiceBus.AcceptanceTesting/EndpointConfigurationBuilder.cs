namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Support;

    public class EndpointConfigurationBuilder : IEndpointConfigurationFactory
    {
        public EndpointConfigurationBuilder()
        {
            configuration.EndpointMappings = new Dictionary<Type, Type>();
        }

        public EndpointConfigurationBuilder AuditTo<T>()
        {
            configuration.AuditEndpoint = typeof(T);
            return this;
        }

        public EndpointConfigurationBuilder AuditTo(Address addressOfAuditQueue)
        {
            configuration.AddressOfAuditQueue = addressOfAuditQueue;
            return this;
        }

        public EndpointConfigurationBuilder CustomMachineName(string customMachineName)
        {
            configuration.CustomMachineName = customMachineName;

            return this;
        }

        public EndpointConfigurationBuilder CustomEndpointName(string customEndpointName)
        {
            configuration.CustomEndpointName = customEndpointName;

            return this;
        }
        

        public EndpointConfigurationBuilder AppConfig(string path)
        {
            configuration.AppConfigPath = path;

            return this;
        }


        public EndpointConfigurationBuilder AddMapping<T>(Type endpoint)
        {
            this.configuration.EndpointMappings.Add(typeof(T),endpoint);

            return this;
        }

        EndpointConfiguration CreateScenario()
        {
            configuration.BuilderType = GetType();

            return this.configuration;
        }

        public EndpointConfigurationBuilder EndpointSetup<T>() where T : IEndpointSetupTemplate
        {
            return EndpointSetup<T>(c => { });
        }

        public EndpointConfigurationBuilder EndpointSetup<T>(Action<Configure> configCustomization) where T: IEndpointSetupTemplate
        {
            configuration.GetConfiguration = (settings,routingTable) =>
                {
                    var config = ((IEndpointSetupTemplate)Activator.CreateInstance<T>()).GetConfiguration(settings, configuration, new ScenarioConfigSource(configuration, routingTable));

                    configCustomization(config);

                    return config;
                };

            return this;
        }

        EndpointConfiguration IEndpointConfigurationFactory.Get()
        {
            return CreateScenario();
        }

        public class SubscriptionsSpy : IAuthorizeSubscriptions
        {
            private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            private int subscriptionsReceived;

            public int NumberOfSubscriptionsToWaitFor { get; set; }

            public bool AuthorizeSubscribe(string messageType, string clientEndpoint,
                                           IDictionary<string, string> headers)
            {
                if (Interlocked.Increment(ref subscriptionsReceived) >= NumberOfSubscriptionsToWaitFor)
                {
                    manualResetEvent.Set();
                }

                return true;
            }

            public bool AuthorizeUnsubscribe(string messageType, string clientEndpoint,
                                             IDictionary<string, string> headers)
            {
                return true;
            }

            public void Wait()
            {
                if(!manualResetEvent.WaitOne(TimeSpan.FromSeconds(20)))
                    throw new Exception("No subscription message was received");

            }
        }


        readonly EndpointConfiguration configuration = new EndpointConfiguration();

        public EndpointConfigurationBuilder WithConfig<T>(Action<T> action)
        {
            var config = Activator.CreateInstance<T>();

            action(config);

            configuration.UserDefinedConfigSections[typeof (T)] = config;
            
            return this;
        }

        public EndpointConfigurationBuilder ExcludeType<T>()
        {
            configuration.TypesToExclude.Add(typeof(T));

            return this;
        }

        public EndpointConfigurationBuilder IncludeType<T>()
        {
            configuration.TypesToInclude.Add(typeof(T));

            return this;
        }
    }
}