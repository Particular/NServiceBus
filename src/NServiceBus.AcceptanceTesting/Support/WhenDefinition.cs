namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

    public class WhenDefinition<TContext> : IWhenDefinition where TContext : ScenarioContext
    {
        public WhenDefinition(Predicate<TContext> condition, Func<IBusSession, Task> action)
        {
            Id = Guid.NewGuid();
            this.condition = condition;
            busAction = action;
        }

        public WhenDefinition(Predicate<TContext> condition, Func<IBusSession, TContext, Task> actionWithContext)
        {
            Id = Guid.NewGuid();
            this.condition = condition;
            busAndContextAction = actionWithContext;
        }

        public Guid Id { get; }

        public async Task<bool> ExecuteAction(ScenarioContext context, IBusSession session)
        {
            var c = (TContext)context;

            if (!condition(c))
            {
                return false;
            }

            if (busAction != null)
            {
                await busAction(session).ConfigureAwait(false);
            }
            else
            {
                await busAndContextAction(session, c).ConfigureAwait(false);
            }

            return true;
        }

        Predicate<TContext> condition;
        Func<IBusSession, Task> busAction;
        Func<IBusSession, TContext, Task> busAndContextAction;
    }
}