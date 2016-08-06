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
        }

        public Type EndpointBuilderType { get; private set; }

        public List<IGivenDefinition> Givens { get; } = new List<IGivenDefinition>();
        public List<IWhenDefinition> Whens { get; } = new List<IWhenDefinition>();
        public List<ICustomConfigDefinition> CustomConfig { get; } = new List<ICustomConfigDefinition>();
        public string AppConfig { get; set; }
    }

    [Serializable]
    public class CustomConfigDefinition<TContext> : ICustomConfigDefinition where TContext : ScenarioContext
    {
        readonly Action<BusConfiguration, TContext> action;

        public CustomConfigDefinition(Action<BusConfiguration, TContext> action)
        {
            this.action = action;
        }

        public void ExecuteAction(BusConfiguration busConfiguration, ScenarioContext context)
        {
            action(busConfiguration, (TContext) context);
        }
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
            var c = context as TContext;

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

    public interface ICustomConfigDefinition
    {
        void ExecuteAction(BusConfiguration busConfiguration, ScenarioContext context);
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