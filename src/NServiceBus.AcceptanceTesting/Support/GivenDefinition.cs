namespace NServiceBus.AcceptanceTesting.Support
{
    using System;

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
}