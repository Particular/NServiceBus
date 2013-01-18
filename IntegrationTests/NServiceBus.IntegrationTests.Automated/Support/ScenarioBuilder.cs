namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Config;

    public class ScenarioBuilder
    {
        public ScenarioBuilder(string name)
        {
            scenario.Givens = new List<Action<IBus>>();
            scenario.Whens = new List<Action<IBus>>();
            scenario.Assertions = new List<Action>();
            scenario.SetupActions = new List<Action<Configure>>();
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
            scenario.Givens.Add(action);

            return this;
        }

        public EndpointScenario CreateScenario()
        {
            return scenario;
        }

        
        public ScenarioBuilder Assert(Action action)
        {
            scenario.Assertions.Add(action);
            return this;
        }

        public ScenarioBuilder Done(Func<bool> func)
        {
            scenario.Done = func;
            return this;
        }

        public ScenarioBuilder When(Action<IBus> func)
        {
            scenario.Whens.Add(func);

            return this;
        }

        readonly EndpointScenario scenario = new EndpointScenario();

        public ScenarioBuilder EndpointSetup<T>() where T: IEndpointSetupTemplate
        {
            scenario.SetupActions.Add(((IEndpointSetupTemplate) Activator.CreateInstance<T>()).GetSetupAction());
            return this;
        }
    }
}