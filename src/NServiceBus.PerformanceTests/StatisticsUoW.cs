namespace Runner
{
    using System;
    using System.Threading;
    using System.Transactions;
    using NServiceBus;
    using NServiceBus.UnitOfWork;

    class StatisticsUoW : IManageUnitsOfWork, INeedInitialization
    {
        public void Begin()
        {
            if (!Statistics.First.HasValue)
            {
                Statistics.First = DateTime.Now;
            }

            if(Transaction.Current != null)
                Transaction.Current.TransactionCompleted += OnCompleted;

        }

        void OnCompleted(object sender, TransactionEventArgs e)
        {
            if (e.Transaction.TransactionInformation.Status != TransactionStatus.Committed)
            {
                return;
            }

            RecordSuccess();
        }

        static void RecordSuccess()
        {
            Statistics.Last = DateTime.Now;
            Interlocked.Increment(ref Statistics.NumberOfMessages);
        }

        public void End(Exception ex = null)
        {
            if (ex != null)
            {
                Interlocked.Increment(ref Statistics.NumberOfRetries);
                return;
            }
                
            if(Transaction.Current == null)
                RecordSuccess();
        }

        public void Init(Configure config)
        {
            config.Configurer.ConfigureComponent<StatisticsUoW>(DependencyLifecycle.InstancePerUnitOfWork);
        }
    }
}