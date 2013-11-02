namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    public class EndpointBehaviorBuilder<TContext> where TContext:ScenarioContext
    {
        
        public EndpointBehaviorBuilder(Type type)
        {
            behaviour = new EndpointBehaviour(type)
                {
                    Givens = new List<IGivenDefinition>(),
                    Whens = new List<IWhenDefinition>()
                };
        }


        public EndpointBehaviorBuilder<TContext> Given(Action<IBus> action)
        {
            behaviour.Givens.Add(new GivenDefinition<TContext>(action));

            return this;
        }


        public EndpointBehaviorBuilder<TContext> Given(Action<IBus,TContext> action)
        {
            behaviour.Givens.Add(new GivenDefinition<TContext>(action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Action<IBus> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Action<IBus> action)
        {
            behaviour.Whens.Add(new WhenDefinition<TContext>(condition,action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Action<IBus,TContext> action)
        {
            behaviour.Whens.Add(new WhenDefinition<TContext>(condition,action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> CustomConfig(Action<Configure> action)
        {
            behaviour.CustomConfig.Add(action);

            return this;
        }

        public EndpointBehaviorBuilder<TContext> AppConfig(string appConfigPath)
        {
            behaviour.AppConfig = appConfigPath;

            return this;
        }


        public EndpointBehaviour Build()
        {
            return behaviour;
        }

        readonly EndpointBehaviour behaviour;
    }
}