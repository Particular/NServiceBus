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
        //[TestCase(TransportTransactionMode.ReceiveOnly)] -- unable to hook where required to throw after a message has been successfully processed but before transaction is successfully committed
        //[TestCase(TransportTransactionMode.SendsAtomicWithReceive)] -- unable to hook where required to throw after a message has been successfully processed but before transaction is successfully committed
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Throwing_during_Transaction_Prepare_should_properly_increment_immediate_processing_failures(TransportTransactionMode transactionMode)
        {
            ErrorContext errorContext = null;

            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    Transaction.Current.EnlistDurable(EnlistmentWhichFailsDuringPrepare.Id, new EnlistmentWhichFailsDuringPrepare(), EnlistmentOptions.None);
                    return Task.CompletedTask;
                },
                (context, _) =>
                {
                    //perform an immediate retry to make sure the transport increments the counter properly
                    if (context.ImmediateProcessingFailures < 2)
                    {
                        return Task.FromResult(ErrorHandleResult.RetryRequired);
                    }

                    errorContext = context;

                    return Task.FromResult(ErrorHandleResult.Discarded);
                },
                (_, __) => completed.SetCompleted(),
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await completed.Task;

            Assert.IsInstanceOf<TransactionAbortedException>(errorContext.Exception);
            Assert.LessOrEqual(2, errorContext.ImmediateProcessingFailures);
        }
    }

    class EnlistmentWhichFailsDuringPrepare : IEnlistmentNotification
    {
        public static readonly Guid Id = Guid.NewGuid();

        // fail during prepare, this will cause scope.Complete to throw
        public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.ForceRollback();

        public void Commit(Enlistment enlistment) => enlistment.Done();

        public void Rollback(Enlistment enlistment) => enlistment.Done();

        public void InDoubt(Enlistment enlistment) => enlistment.Done();
    }
}