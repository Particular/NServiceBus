namespace Subscriber1
{
    using System;
    using System.Transactions;

    using MyMessages.Publisher;
    using MyMessages.Subscriber1;
    using MyMessages.SubscriberNMS;

    using NServiceBus;
    using NServiceBus.Logging;

    public class CommandMessageHandler : IHandleMessages<MyRequest1>
    {
        private readonly IBus bus;
        private static TestSinglePhaseCommit x = new TestSinglePhaseCommit();
        private static Guid guid = Guid.NewGuid();

        public CommandMessageHandler(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(MyRequest1 message)
        {
            Transaction.Current.EnlistDurable(guid, x, EnlistmentOptions.None);
            Logger.Info(string.Format("Subscriber 1 received MyRequest1 with Id {0}.", message.CommandId));
            Logger.Info(string.Format("Message time: {0}.", message.Time));
            Logger.Info(string.Format("Message duration: {0}.", message.Duration));

            var result = new Random().Next(2) == 1 ? ResponseCode.Ok : ResponseCode.Failed;
            this.bus.Return(result);
            Console.WriteLine("Replied with response {0}", result);

            if (message.ThrowExceptionDuringProcessing)
            {
                Console.WriteLine("Throwing Exception");
                throw new Exception();
            }

            Console.WriteLine("==========================================================================");
        }

        internal class TestSinglePhaseCommit : ISinglePhaseNotification
        {
            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                Console.WriteLine("Tx Prepare");
                preparingEnlistment.Prepared();
            }
            public void Commit(Enlistment enlistment)
            {
                Console.WriteLine("Tx Commit");
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                Console.WriteLine("Tx Rollback");
                enlistment.Done();
            }
            public void InDoubt(Enlistment enlistment)
            {
                Console.WriteLine("Tx InDoubt");
                enlistment.Done();
            }
            public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
            {
                Console.WriteLine("Tx SinglePhaseCommit");
                singlePhaseEnlistment.Committed();
                //singlePhaseEnlistment.Aborted();
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandMessageHandler));
    }
}