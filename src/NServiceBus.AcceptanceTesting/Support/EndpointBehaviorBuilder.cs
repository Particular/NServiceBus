namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    public class EndpointBehaviorBuilder<TContext> where TContext:ScenarioContext
    {
        
        public EndpointBehaviorBuilder(Type type)
        {
            behavior = new EndpointBehavior(type)
                {
                    Givens = new List<IGivenDefinition>(),
                    Whens = new List<IWhenDefinition>()
                };
        }


        public EndpointBehaviorBuilder<TContext> Given(Action<IBus> action)
        {
            behavior.Givens.Add(new GivenDefinition<TContext>(action));

            return this;
        }


        public EndpointBehaviorBuilder<TContext> Given(Action<IBus,TContext> action)
        {
            behavior.Givens.Add(new GivenDefinition<TContext>(action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Action<IBus> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Action<IBus> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(condition,action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Action<IBus,TContext> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(condition,action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> CustomConfig(Action<BusConfiguration> action)
        {
            behavior.CustomConfig.Add(action);

            return this;
        }

        public EndpointBehaviorBuilder<TContext> AppConfig(string appConfig)
        {
            behavior.AppConfig = appConfig;

            return this;
        }

        public EndpointBehavior Build()
        {
            return behavior;
        }

        readonly EndpointBehavior behavior;
    }
}