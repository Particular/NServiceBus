namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Config;

    public class ScenarioBuilder
    {
        public ScenarioBuilder(string name)
        {
            this.behavior.Givens = new List<Action<IBus>>();
            this.behavior.Whens = new List<Action<IBus>>();
            this.behavior.Setups = new List<Action<IDictionary<string, string>, Configure>>();
            this.behavior.EndpointMappings = new MessageEndpointMappingCollection();
            this.behavior.EndpointName = name;
            this.behavior.Done = context => true;
        }

        public ScenarioBuilder AddMapping<T>(string endpoint)
        {
            this.behavior.EndpointMappings.Add(new MessageEndpointMapping
                {
                    AssemblyName = typeof(T).Assembly.FullName,
                    TypeFullName = typeof(T).FullName,Endpoint = endpoint
                });

            return this;
        }


        public ScenarioBuilder Given(Action<IBus> action)
        {
            this.behavior.Givens.Add(action);

            return this;
        }

        public EndpointBehavior CreateScenario()
        {
            return this.behavior;
        }

        public ScenarioBuilder Done<TContext>(Func<TContext, bool> func)
            where TContext : BehaviorContext
        {
            this.behavior.Done = context => func((TContext)context);
            return this;
        }

        public ScenarioBuilder Done(Func<bool> func)
        {
            this.behavior.Done = context => func();
            return this;
        }

        public ScenarioBuilder When(Action<IBus> func)
        {
            this.behavior.Whens.Add(func);

            return this;
        }

        readonly EndpointBehavior behavior = new EndpointBehavior();

        public ScenarioBuilder EndpointSetup<T>() where T : IEndpointSetupTemplate
        {
            this.behavior.Setups.Add((settings, conf) => ((IEndpointSetupTemplate)Activator.CreateInstance<T>()).Setup(conf, settings));
            return this;
        }
        public ScenarioBuilder EndpointSetup<T>(Action<Configure> configCustomization) where T: IEndpointSetupTemplate
        {
            this.behavior.Setups.Add((settings, conf) =>
                {
                    ((IEndpointSetupTemplate) Activator.CreateInstance<T>()).Setup(conf, settings);

                    configCustomization(conf);
                });
            return this;
        }
    }
}