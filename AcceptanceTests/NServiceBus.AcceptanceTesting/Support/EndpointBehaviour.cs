namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Customization;

    [Serializable]
    public class EndpointBehaviour : MarshalByRefObject
    {
        public EndpointBehaviour(Type builderType)
        {
            EndpointBuilderType = builderType;
            EndpointName = Conventions.EndpointNamingConvention(builderType);
            CustomConfig = new List<Action<Configure>>();
        }

        public string EndpointName { get; private set; }

        public Type EndpointBuilderType { get; private set; }



        public List<IGivenDefinition> Givens { get; set; }
        public List<IWhenDefinition> Whens { get; set; }

        public List<Action<Configure>> CustomConfig { get; set; }
    }


    [Serializable]
    public class GivenDefinition<TContext> : IGivenDefinition where TContext : ScenarioContext
    {
        public GivenDefinition(Action<IBus> action)
        {
            givenAction2 = action;
        }

        public GivenDefinition(Action<IBus, TContext> action)
        {
            givenAction = action;
        }

        public Action<IBus> GetAction(ScenarioContext context)
        {
            if (givenAction2 != null)
                return bus => givenAction2(bus);

            return bus => givenAction(bus, (TContext)context);
        }

        readonly Action<IBus, TContext> givenAction;
        readonly Action<IBus> givenAction2;

    }

    [Serializable]
    public class WhenDefinition<TContext> : IWhenDefinition where TContext : ScenarioContext
    {
        public WhenDefinition(Predicate<TContext> condition, Action<IBus> action)
        {
            this.condition = condition;
            busAction = action;
        }

        public WhenDefinition(Predicate<TContext> condition, Action<IBus, TContext> actionWithContext)
        {
            this.condition = condition;
            busAndContextAction = actionWithContext;
        }

        public void ExecuteAction(ScenarioContext context, IBus bus)
        {
            var c = context as TContext;

            if (executed)
            {
                return;
            }

            if (!condition(c))
            {
                return;
            }

            lock (lockObj)
            {
                if (executed)
                {
                    return;
                }

                if (busAction != null)
                {
                    busAction(bus);
                    executed = true;
                    return;
                }

                busAndContextAction(bus, c);
                executed = true;
            }
        }

        readonly Predicate<TContext> condition;
        readonly Action<IBus> busAction;
        readonly Action<IBus, TContext> busAndContextAction;
        readonly object lockObj = new object();
        bool executed;
    }

    public interface IGivenDefinition
    {
        Action<IBus> GetAction(ScenarioContext context);
    }


    public interface IWhenDefinition
    {
        void ExecuteAction(ScenarioContext context, IBus bus);

    }
}