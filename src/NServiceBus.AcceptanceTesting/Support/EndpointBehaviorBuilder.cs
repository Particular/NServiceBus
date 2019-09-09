namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class EndpointBehaviorBuilder<TContext> where TContext : ScenarioContext
    {
        public EndpointBehaviorBuilder(Type type)
        {
            behavior = new EndpointBehavior(type)
            {
                Whens = new List<IWhenDefinition>()
            };
        }

        public EndpointBehaviorBuilder<TContext> When(Func<IMessageSession, TContext, Task> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Func<IMessageSession, Task> action)
        {
            return When(c => true, action);
        }

        public EndpointBehaviorBuilder<TContext> When(Func<TContext, Task<bool>> condition, Func<IMessageSession, Task> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IMessageSession, Task> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(ctx => Task.FromResult(condition(ctx)), action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Func<TContext, Task<bool>> condition, Func<IMessageSession, TContext, Task> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IMessageSession, TContext, Task> action)
        {
            behavior.Whens.Add(new WhenDefinition<TContext>(ctx => Task.FromResult(condition(ctx)), action));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> CustomConfig(Action<EndpointConfiguration> action)
        {
            behavior.CustomConfig.Add((busConfig, context) => action(busConfig));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> CustomConfig(Action<EndpointConfiguration, TContext> action)
        {
            behavior.CustomConfig.Add((configuration, context) => action(configuration, (TContext)context));

            return this;
        }

        public EndpointBehaviorBuilder<TContext> ToCreateInstance<T>(Func<EndpointConfiguration, Task<T>> createCallback, Func<T, Task<IEndpointInstance>> startCallback)
        {
            behavior.ConfigureHowToCreateInstance(createCallback, startCallback);

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
