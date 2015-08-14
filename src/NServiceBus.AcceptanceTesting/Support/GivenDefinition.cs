namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

    [Serializable]
    public class GivenDefinition<TContext> : IGivenDefinition where TContext : ScenarioContext
    {
        public GivenDefinition(Func<IBus, Task> func)
        {
            givenAction2 = func;
        }

        public GivenDefinition(Func<IBus, TContext, Task> func)
        {
            givenAction = func;
        }

        public Func<IBus, Task> GetFunction(ScenarioContext context)
        {
            if (givenAction2 != null)
                return bus => givenAction2(bus);

            return bus => givenAction(bus, (TContext)context);
        }

        readonly Func<IBus, TContext, Task> givenAction;
        readonly Func<IBus, Task> givenAction2;

    }
}