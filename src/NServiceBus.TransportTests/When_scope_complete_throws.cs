﻿namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    public class When_scope_complete_throws : NServiceBusTransportTest
    {
        //[TestCase(TransportTransactionMode.None)] -- not relevant
        //[TestCase(TransportTransactionMode.ReceiveOnly)] -- unable to hook where required to throw after a message has been successfully processed but before transaction is successfully committed
        //[TestCase(TransportTransactionMode.SendsAtomicWithReceive)] -- unable to hook where required to throw after a message has been successfully processed but before transaction is successfully committed
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_call_on_error(TransportTransactionMode transactionMode)
        {
            var onErrorCalled = CreateTaskCompletionSource<ErrorContext>();

            await StartPump(
                (_, __) =>
                {
                    // handler enlists a failing transaction enlistment to the DTC transaction which will fail when committing the transaction.
                    Transaction.Current.EnlistDurable(EnlistmentWhichFailsDuringPrepare.Id, new EnlistmentWhichFailsDuringPrepare(), EnlistmentOptions.None);
                    return Task.CompletedTask;
                },
                (context, _) =>
                {
                    onErrorCalled.SetResult(context);
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            var errorContext = await onErrorCalled.Task;

            Assert.IsInstanceOf<TransactionAbortedException>(errorContext.Exception);

            // since some transports doesn't have native retry counters we can't expect the attempts to be fully consistent since if
            // dispose throws the message might be picked up before the counter is incremented
            Assert.LessOrEqual(1, errorContext.ImmediateProcessingFailures);
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
}
