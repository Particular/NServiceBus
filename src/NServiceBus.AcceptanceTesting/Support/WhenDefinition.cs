namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

    public class WhenDefinition<TContext> : IWhenDefinition where TContext : ScenarioContext
    {
        public WhenDefinition(Predicate<TContext> condition, Func<IBusSessionFactory, Task> action)
        {
            Id = Guid.NewGuid();
            this.condition = condition;
            busAction = action;
        }

        public WhenDefinition(Predicate<TContext> condition, Func<IBusSessionFactory, TContext, Task> actionWithContext)
        {
            Id = Guid.NewGuid();
            this.condition = condition;
            busAndContextAction = actionWithContext;
        }

        public Guid Id { get; }

        public async Task<bool> ExecuteAction(ScenarioContext context, IBusSessionFactory sessionFactory)
        {
            var c = (TContext)context;

            if (!condition(c))
            {
                return false;
            }

            if (busAction != null)
            {
                await busAction(sessionFactory).ConfigureAwait(false);
            }
            else
            {
                await busAndContextAction(sessionFactory, c).ConfigureAwait(false);
            }

            return true;
        }

        Predicate<TContext> condition;
        Func<IBusSessionFactory, Task> busAction;
        Func<IBusSessionFactory, TContext, Task> busAndContextAction;
    }
}