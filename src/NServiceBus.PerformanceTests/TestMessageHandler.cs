namespace Runner
{
    using System;
    using System.Threading;
    using System.Transactions;

    using NServiceBus;

    class TestMessageHandler:IHandleMessages<TestMessage>
    {
        private static TwoPhaseCommitEnlistment enlistment = new TwoPhaseCommitEnlistment();

        public void Handle(TestMessage message)
        {
            if (!Statistics.First.HasValue)
            {
                Statistics.First = DateTime.Now;
            }
            Interlocked.Increment(ref Statistics.NumberOfMessages);

            if (message.TwoPhaseCommit)
            {
                Transaction.Current.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);
            }

            Statistics.Last = DateTime.Now;
        }
    }
}