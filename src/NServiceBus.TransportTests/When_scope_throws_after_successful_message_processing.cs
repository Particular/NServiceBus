namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    public class When_scope_throws_after_successful_message_processing : NServiceBusTransportTest
    {
        //[TestCase(TransportTransactionMode.None)] -- not relevant
        //[TestCase(TransportTransactionMode.ReceiveOnly)] -- unable to hook where required to throw after a message has been successfully processed but before transaction is successfully commited
        //[TestCase(TransportTransactionMode.SendsAtomicWithReceive)] -- unable to hook where required to throw after a message has been successfully processed but before transaction is successfully commited
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_properly_increment_immediate_processing_failures(TransportTransactionMode transactionMode)
        {
            var onErrorCalled = new TaskCompletionSource<ErrorContext>();

            OnTestTimeout(() => onErrorCalled.SetCanceled());

            await StartPump(context =>
            {
                Transaction.Current.EnlistDurable(EnlistmentWhichFailesDuringPrepare.Id, new EnlistmentWhichFailesDuringPrepare(), EnlistmentOptions.None);
                return Task.FromResult(0);
            },
            context =>
            {
                //perform an immediate retry to make sure the transport increments the counter properly
                if (context.ImmediateProcessingFailures < 2)
                {
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                }
                onErrorCalled.SetResult(context);

                return Task.FromResult(ErrorHandleResult.Handled);
            }
            , transactionMode);

            await SendMessage(InputQueueName);

            var errorContext = await onErrorCalled.Task;

            Assert.IsInstanceOf<TransactionAbortedException>(errorContext.Exception);

            // since some transports doesn't have native retry counters we can't expect the attempts to be fully consistent since if
            // dispose throws the message might be picked up before the counter is incremented
            Assert.LessOrEqual(2, errorContext.ImmediateProcessingFailures);
        }
    }

    class EnlistmentWhichFailesDuringPrepare : IEnlistmentNotification
    {
        public static readonly Guid Id = Guid.NewGuid();

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            // fail during prepare, this will cause scope.Dispose to throw
            preparingEnlistment.ForceRollback();
        }

        public void Commit(Enlistment enlistment)
        {
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
    }
}