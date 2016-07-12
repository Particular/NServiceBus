namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    public class When_scope_dispose_throws : NServiceBusTransportTest
    {
        [Test]
        public async Task Should_call_on_error()
        {
            var onErrorCalled = new TaskCompletionSource<ErrorContext>();

            OnTestTimeout(() => onErrorCalled.SetResult(null));

            await StartPump(context =>
            {
                // handler enlists a failing transaction enlistment to the DTC transaction which will fail when commiting the transaction.
                Transaction.Current.EnlistDurable(EnlistmentWhichFailesDuringPrepare.Id, new EnlistmentWhichFailesDuringPrepare(), EnlistmentOptions.None);

                return Task.FromResult(0);
            },
            context =>
            {
                onErrorCalled.SetResult(context);
                return Task.FromResult(ErrorHandleResult.Handled);
            }
            ,TransportTransactionMode.TransactionScope);

            await SendMessage(InputQueueName);

            var errorContext = await onErrorCalled.Task;

            Assert.IsInstanceOf<TransactionAbortedException>(errorContext.Exception);

            // since some transports doesn't have native retry counters we can't expect the attempts to be fully consistent since if
            // dispose throws the message might be picked up before the counter is incremented
            Assert.LessOrEqual(1, errorContext.NumberOfDeliveryAttempts);
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