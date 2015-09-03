namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

    [Serializable]
    public class GivenDefinition<TContext> : IGivenDefinition where TContext : ScenarioContext
    {
        public GivenDefinition(Func<IBus, Task> action)
        {
            givenAction2 = action;
        }

        public GivenDefinition(Func<IBus, TContext, Task> action)
        {
            givenAction = action;
        }

        public Func<IBus, Task> GetAction(ScenarioContext context)
        {
            if (givenAction2 != null)
                return bus => givenAction2(bus);

            return bus => givenAction(bus, (TContext)context);
        }

        Func<IBus, TContext, Task> givenAction;
        Func<IBus, Task> givenAction2;
    }
}