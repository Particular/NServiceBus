namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

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
        public string AppConfig { get; set; }
    }

    [Serializable]
    public class WhenDefinition<TContext> : IWhenDefinition where TContext : ScenarioContext
    {
        public WhenDefinition(Predicate<TContext> condition, Action<IBus> action)
        {
            id = Guid.NewGuid();
            this.condition = condition;
            busAction = action;
        }

        public WhenDefinition(Predicate<TContext> condition, Action<IBus, TContext> actionWithContext)
        {
            id = Guid.NewGuid();
            this.condition = condition;
            busAndContextAction = actionWithContext;
        }

        public Guid Id { get { return id; } }

        public bool ExecuteAction(ScenarioContext context, IBus bus)
        {
            var c = (TContext)context;

            if (!condition(c))
            {
                return false;
            }


            if (busAction != null)
            {
                busAction(bus);
            }
            else
            {
                busAndContextAction(bus, c);
          
            }

            Debug.WriteLine("Condition {0} has fired - Thread: {1} AppDomain: {2}", id, Thread.CurrentThread.ManagedThreadId,AppDomain.CurrentDomain.FriendlyName);

            return true;
        }

        readonly Predicate<TContext> condition;
        readonly Action<IBus> busAction;
        readonly Action<IBus, TContext> busAndContextAction;
        Guid id;
    }

    public interface IGivenDefinition
    {
        Action<IBus> GetAction(ScenarioContext context);
    }


    public interface IWhenDefinition
    {
        bool ExecuteAction(ScenarioContext context, IBus bus);

        Guid Id { get; }
    }
}