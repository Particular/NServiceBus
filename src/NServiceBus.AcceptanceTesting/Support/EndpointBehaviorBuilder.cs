namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class EndpointBehaviorBuilder<TContext> where TContext:ScenarioContext
    {
        
        public EndpointBehaviorBuilder(Type type)
        {
            behavior = new EndpointBehavior(type)
                {
                    Whens = new List<IWhenDefinition>()
                };
        }

        public EndpointBehaviorBuilder<TContext> When(Func<IBusContext, TContext, Task> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Func<IBusContext, Task> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IBusContext, Task> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IBusContext, TContext, Task> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(condition,action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> CustomConfig(Action<BusConfiguration> action)
        {
            behavior.CustomConfig.Add(action);

            return this;
        }

        public EndpointBehaviorBuilder<TContext> DoNotFailOnErrorMessages()
        {
            behavior.DoNotFailOnErrorMessages = true;

            return this;
        }

        public EndpointBehavior Build()
        {
            return behavior;
        }

        EndpointBehavior behavior;
    }
}