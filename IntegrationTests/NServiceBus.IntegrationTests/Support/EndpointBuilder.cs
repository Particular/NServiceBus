namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Generic;
    using Config;

    public class EndpointBuilder : IEndpointBehaviorFactory
    {
        public EndpointBuilder()
        {
            behavior.Givens = new List<Action<IBus>>();
            behavior.Whens = new List<Action<IBus>>();
            behavior.EndpointMappings = new Dictionary<Type, Type>();
            behavior.Done = context => true;
        }
     
        public EndpointBuilder AddMapping<T>(Type endpoint)
        {
            this.behavior.EndpointMappings.Add(typeof(T),endpoint);

            return this;
        }


        public EndpointBuilder Given(Action<IBus> action)
        {
            this.behavior.Givens.Add(action);

            return this;
        }

        EndpointBehavior CreateScenario()
        {
            return this.behavior;
        }

        public EndpointBuilder Done<TContext>(Func<TContext, bool> func)
            where TContext : BehaviorContext
        {
            this.behavior.Done = context => func((TContext)context);
            return this;
        }

        public EndpointBuilder Done(Func<bool> func)
        {
            this.behavior.Done = context => func();
            return this;
        }

        public EndpointBuilder When(Action<IBus> func)
        {
            this.behavior.Whens.Add(func);

            return this;
        }

        readonly EndpointBehavior behavior = new EndpointBehavior();

        public EndpointBuilder EndpointSetup<T>() where T : IEndpointSetupTemplate
        {
            return EndpointSetup<T>(c => { });
        }

        public EndpointBuilder EndpointSetup<T>(Action<Configure> configCustomization) where T: IEndpointSetupTemplate
        {
            behavior.GetConfiguration = (settings,routingTable) =>
                {
                    var config = ((IEndpointSetupTemplate)Activator.CreateInstance<T>()).GetConfiguration(settings, behavior, new ScenarioConfigSource(behavior, routingTable));

                    configCustomization(config);

                    return config;
                };

            return this;
        }



        EndpointBehavior IEndpointBehaviorFactory.Get()
        {
            return CreateScenario();
        }

    }
}