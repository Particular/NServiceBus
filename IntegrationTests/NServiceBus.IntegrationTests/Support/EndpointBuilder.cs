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
            behavior.Setups = new List<Action<IDictionary<string, string>, Configure>>();
            behavior.EndpointMappings = new MessageEndpointMappingCollection();
            behavior.EndpointName = Conventions.EndpointNamingConvention(GetType());
            behavior.Done = context => true;
            
        }
     
        public EndpointBuilder AddMapping<T>(string endpoint)
        {
            this.behavior.EndpointMappings.Add(new MessageEndpointMapping
                {
                    AssemblyName = typeof(T).Assembly.FullName,
                    TypeFullName = typeof(T).FullName,Endpoint = endpoint
                });

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


        public EndpointBuilder Name(string name)
        {
            this.behavior.EndpointName = name;
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
            this.behavior.Setups.Add((settings, conf) => ((IEndpointSetupTemplate)Activator.CreateInstance<T>()).Setup(conf, settings));
            return this;
        }
        public EndpointBuilder EndpointSetup<T>(Action<Configure> configCustomization) where T: IEndpointSetupTemplate
        {
            this.behavior.Setups.Add((settings, conf) =>
                {
                    ((IEndpointSetupTemplate) Activator.CreateInstance<T>()).Setup(conf, settings);

                    configCustomization(conf);
                });
            return this;
        }

        EndpointBehavior IEndpointBehaviorFactory.Get()
        {
            return CreateScenario();
        }

    }
}