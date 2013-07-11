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
            if (!Timings.First.HasValue)
            {
                Timings.First = DateTime.Now;
            }
            Interlocked.Increment(ref Timings.NumberOfMessages);

            if (message.TwoPhaseCommit)
            {
                Transaction.Current.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);
            }

            Timings.Last = DateTime.Now;
        }
    }
}