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

        public EndpointBehaviorBuilder<TContext> When(Func<TContext, Task> action)
        {
            return When(c => true, (IBusSessionFactory f, TContext c) => action(c));
        }

        public EndpointBehaviorBuilder<TContext> When(Func<Task> action)
        {
            return When(c => true, (IBusSessionFactory f) => action());
        }

        public EndpointBehaviorBuilder<TContext> When(Func<IBusSession, TContext, Task> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Func<IBusSession, Task> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IBusSession, Task> action)
        {
            return When(condition, f => action(f.CreateBusSession()));
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IBusSession, TContext, Task> action)
        {
            return When(condition, (f, c) => action(f.CreateBusSession(), c));
        }

        public EndpointBehaviorBuilder<TContext> When(Func<IBusSessionFactory, TContext, Task> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Func<IBusSessionFactory, Task> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IBusSessionFactory, Task> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IBusSessionFactory, TContext, Task> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> CustomConfig(Action<BusConfiguration> action)
        {
            behavior.CustomConfig.Add((busConfig, context) => action(busConfig));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> CustomConfig(Action<BusConfiguration, TContext> action)
        {
            behavior.CustomConfig.Add(((configuration, context) => action(configuration, (TContext) context)));

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