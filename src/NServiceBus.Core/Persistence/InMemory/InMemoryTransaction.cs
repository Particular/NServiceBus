namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    class InMemoryTransaction
    {
        List<Action> actions = new List<Action>();

        public void Enlist(Action action)
        {
            actions.Add(action);
        }
        
        public void Commit()
        {
            foreach (var action in actions)
            {
                action();
            }
            actions.Clear();
        }
    }
}