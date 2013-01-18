namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Config;

    public class ScenarioBuilder
    {
        public ScenarioBuilder(string name)
        {
            behaviour.Givens = new List<Action<IBus>>();
            behaviour.Whens = new List<Action<IBus>>();
            behaviour.Assertions = new List<Action>();
            behaviour.SetupActions = new List<Action<Configure>>();
            behaviour.EndpointMappings = new MessageEndpointMappingCollection();
            behaviour.EndpointName = name;
            behaviour.Done = () => true;
        }

        public ScenarioBuilder AddMapping<T>(string endpoint)
        {
            behaviour.EndpointMappings.Add(new MessageEndpointMapping
                {
                    AssemblyName = typeof(T).Assembly.FullName,
                    TypeFullName = typeof(T).FullName,Endpoint = endpoint
                });

            return this;
        }


        public ScenarioBuilder Given(Action<IBus> action)
        {
            behaviour.Givens.Add(action);

            return this;
        }

        public EndpointBehaviour CreateScenario()
        {
            return behaviour;
        }

        
        public ScenarioBuilder Assert(Action action)
        {
            behaviour.Assertions.Add(action);
            return this;
        }

        public ScenarioBuilder Done(Func<bool> func)
        {
            behaviour.Done = func;
            return this;
        }

        public ScenarioBuilder When(Action<IBus> func)
        {
            behaviour.Whens.Add(func);

            return this;
        }

        readonly EndpointBehaviour behaviour = new EndpointBehaviour();

        public ScenarioBuilder EndpointSetup<T>() where T: IEndpointSetupTemplate
        {
            behaviour.SetupActions.Add(((IEndpointSetupTemplate) Activator.CreateInstance<T>()).GetSetupAction());
            return this;
        }
    }
}