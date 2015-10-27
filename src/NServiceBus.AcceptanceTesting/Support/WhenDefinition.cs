namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

    public class WhenDefinition<TContext> : IWhenDefinition where TContext : ScenarioContext
    {
        public WhenDefinition(Predicate<TContext> condition, Func<IBusContext, Task> action)
        {
            Id = Guid.NewGuid();
            this.condition = condition;
            busAction = action;
        }

        public WhenDefinition(Predicate<TContext> condition, Func<IBusContext, TContext, Task> actionWithContext)
        {
            Id = Guid.NewGuid();
            this.condition = condition;
            busAndContextAction = actionWithContext;
        }

        public Guid Id { get; }

        public async Task<bool> ExecuteAction(ScenarioContext context, IBusContext bus)
        {
            var c = (TContext)context;

            if (!condition(c))
            {
                return false;
            }

            if (busAction != null)
            {
                await busAction(bus).ConfigureAwait(false);
            }
            else
            {
                await busAndContextAction(bus, c).ConfigureAwait(false);
            }

            return true;
        }

        Predicate<TContext> condition;
        Func<IBusContext, Task> busAction;
        Func<IBusContext, TContext, Task> busAndContextAction;
    }
}