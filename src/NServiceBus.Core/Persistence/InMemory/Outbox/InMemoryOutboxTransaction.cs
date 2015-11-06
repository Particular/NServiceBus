namespace NServiceBus.InMemory.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Outbox;

    [SkipWeaving]
    class InMemoryOutboxTransaction : OutboxTransaction
    {
        List<Action> actions = new List<Action>();
         
        public void Enlist(Action action)
        {
            actions.Add(action);
        }

        public void Dispose()
        {
            actions.Clear();        
        }

        public Task Commit()
        {
            foreach (var action in actions)
            {
                action();
            }
            return TaskEx.Completed;
        }
    }
}