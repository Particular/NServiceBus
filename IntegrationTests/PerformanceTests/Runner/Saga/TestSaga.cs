namespace Runner.Saga
{
    using System;
    using System.Threading;
    using System.Transactions;

    using NServiceBus;
    using NServiceBus.Saga;

    class TestSaga : Saga<SagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<CompleteSagaMessage>
    {
        private static TwoPhaseCommitEnlistment enlistment = new TwoPhaseCommitEnlistment();
        public override void ConfigureHowToFindSaga()
        {
            this.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.Number);
            this.ConfigureMapping<CompleteSagaMessage>(m => m.Id).ToSaga(s => s.Number);
        }

        public void Handle(StartSagaMessage message)
        {
            this.BeforeMessage(message);
            this.Data.Number = message.Id;
            this.AfterMessage();
        }

        public void Handle(CompleteSagaMessage message)
        {
            this.BeforeMessage(message);
            this.MarkAsComplete();
            this.AfterMessage();
        }

        private void AfterMessage()
        {
            Timings.Last = DateTime.Now;
        }

        private void BeforeMessage(MessageBase message)
        {
            if (!Timings.First.HasValue)
            {
                Timings.First = DateTime.Now;
            }
            Interlocked.Increment(ref Timings.NumberOfMessages);

            if (message.TwoPhaseCommit)
            {
                Transaction.Current.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);
            }
        }
    }
}