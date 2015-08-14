namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    [Serializable]
    public class EndpointBehavior : MarshalByRefObject
    {
        public EndpointBehavior(Type builderType)
        {
            EndpointBuilderType = builderType;
            CustomConfig = new List<Action<BusConfiguration>>();
        }

        public Type EndpointBuilderType { get; private set; }

        public List<IGivenDefinition> Givens { get; set; }
        public List<IWhenDefinition> Whens { get; set; }

        public List<Action<BusConfiguration>> CustomConfig { get; set; }
    }

    [Serializable]
    public class WhenDefinition<TContext> : IWhenDefinition where TContext : ScenarioContext
    {
        public WhenDefinition(Predicate<TContext> condition, Func<IBus, Task> action)
        {
            Id = Guid.NewGuid();
            this.condition = condition;
            busAction = action;
        }

        public WhenDefinition(Predicate<TContext> condition, Func<IBus, TContext, Task> actionWithContext)
        {
            Id = Guid.NewGuid();
            this.condition = condition;
            busAndContextAction = actionWithContext;
        }

        public Guid Id { get; private set; }

        public async Task<bool> ExecuteAction(ScenarioContext context, IBus bus)
        {
            var c = (TContext)context;

            if (!condition(c))
            {
                return false;
            }


            if (busAction != null)
            {
                await busAction(bus);
            }
            else
            {
                await busAndContextAction(bus, c);
          
            }

            Debug.WriteLine("Condition {0} has fired - Thread: {1} AppDomain: {2}", Id, Thread.CurrentThread.ManagedThreadId,AppDomain.CurrentDomain.FriendlyName);

            return true;
        }

        Predicate<TContext> condition;
        readonly Func<IBus, Task> busAction;
        readonly Func<IBus, TContext, Task> busAndContextAction;
    }

    public interface IGivenDefinition
    {
        Func<IBus, Task> GetFunction(ScenarioContext context);
    }


    public interface IWhenDefinition
    {
        Task<bool> ExecuteAction(ScenarioContext context, IBus bus);

        Guid Id { get; }
    }
}