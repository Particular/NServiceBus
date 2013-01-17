namespace NServiceBus.IntegrationTests.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Config;

    public class ScenarioBuilder
    {
        public ScenarioBuilder(string name)
        {
            scenario.InitialBusActions = new List<Action<IBus>>();
            scenario.EndpointMappings = new MessageEndpointMappingCollection();
            scenario.EndpointName = name;
            scenario.Done = () => true;
        }

        public ScenarioBuilder AddMapping<T>(string endpoint)
        {
            scenario.EndpointMappings.Add(new MessageEndpointMapping
                {
                    AssemblyName = typeof(T).Assembly.FullName,
                    TypeFullName = typeof(T).FullName,Endpoint = endpoint
                });

            return this;
        }


        public ScenarioBuilder Given(Action<IBus> action)
        {
            scenario.InitialBusActions.Add(action);

            return this;
        }

        public EndpointScenario CreateScenario()
        {
            return scenario;
        }

        EndpointScenario scenario = new EndpointScenario();

        public ScenarioBuilder RegisterHandler<T>()
        {
            return this;
        }

        public ScenarioBuilder Assert(Action action)
        {
            return this;
        }

        public ScenarioBuilder Done(Func<bool> func)
        {
            scenario.Done = func;
            return this;
        }
    }
}