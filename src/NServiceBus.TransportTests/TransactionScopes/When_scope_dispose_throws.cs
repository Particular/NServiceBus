namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transports;

    public class When_scope_dispose_throws : ScopeTransportTest
    {
        [Test]
        public async Task Should_call_on_error()
        {
            var onErrorTsc = new TaskCompletionSource<ErrorContext>();

            await StartPump(context =>
            {
                // handler enlists a failing transaction enlistment to the DTC transaction which will fail when commiting the transaction.
                Transaction.Current.EnlistDurable(EnlistmentWhichFailesDuringPrepare.Id, new EnlistmentWhichFailesDuringPrepare(), EnlistmentOptions.None);

                return Task.FromResult(0);
            },
            context =>
            {
                onErrorTsc.SetResult(context);
                return Task.FromResult(false);
            });

            await SendMessage(InputQueueName);

            var errorContext = await onErrorTsc.Task;

            Assert.IsInstanceOf<TransactionAbortedException>(errorContext.Exception);
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