namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Customization;
    using Serializers.Json;

    [Serializable]
    public class EndpointBehaviour : MarshalByRefObject
    {
        public EndpointBehaviour(Type builderType)
        {
            EndpointBuilderType = builderType;
            EndpointName = Conventions.EndpointNamingConvention(builderType);
        }

        public string EndpointName { get; private set; }

        public Type EndpointBuilderType { get; private set; }



        public List<IGivenDefinition> Givens { get; set; }
        public List<IWhenDefinition> Whens { get; set; }
    }

   
    [Serializable]
    public class GivenDefinition<TContext> : IGivenDefinition where TContext : ScenarioContext
    {
        public GivenDefinition(Action<IBus> action)
        {
            givenAction2 = action;
        }

        public GivenDefinition(Action<IBus,TContext> action)
        {
            givenAction = action;
        }

        public Action<IBus> GetAction(ScenarioContext context)
        {
            if(givenAction2 != null)
                return bus => givenAction2(bus);

            return bus => givenAction(bus, (TContext) context);
        }

        readonly Action<IBus,TContext> givenAction;
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

      
        public Action<IBus> GetAction(ScenarioContext context)
        {
            var c = context as TContext;

            if (!condition(c))
            {
            
                return bus => { Debug.Write("Condition is false"); };
            }

            if (busAction != null)
                return busAction;

            return bus => busAndContextAction(bus,c);
        }

        readonly Predicate<TContext> condition;

        readonly Action<IBus> busAction;

        readonly Action<IBus, TContext> busAndContextAction;
    }

    public interface IGivenDefinition
    {
        Action<IBus> GetAction(ScenarioContext context);
    }


   public interface IWhenDefinition
    {
        Action<IBus> GetAction(ScenarioContext context);

    }
}