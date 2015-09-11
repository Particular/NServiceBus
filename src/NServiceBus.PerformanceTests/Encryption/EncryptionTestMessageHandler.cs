namespace Runner.Encryption
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;

    using NServiceBus;

    class TestMessageHandler : IHandleMessages<EncryptionTestMessage>
    {
        static TwoPhaseCommitEnlistment enlistment = new TwoPhaseCommitEnlistment();

        public Task Handle(EncryptionTestMessage message)
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

            return Task.FromResult(0);
        }
    }
}