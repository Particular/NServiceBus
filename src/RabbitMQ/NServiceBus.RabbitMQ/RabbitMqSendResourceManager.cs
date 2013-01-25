namespace NServiceBus.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;

  
    public class RabbitMqSendResourceManager : IEnlistmentNotification
    {
        readonly IList<Action> actions = new List<Action>();

      
        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            foreach (var action in actions)
            {
                action();
            }

            actions.Clear();

            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void Add(Action action)
        {
           actions.Add(action);
        }
    }
}