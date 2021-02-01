namespace NServiceBus.TransportTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_user_aborts_processing : NServiceBusTransportTest
    {
        //[TestCase(TransportTransactionMode.None)] - Not currently supported since we can't rollback the message. We could consider inmemory retry similar to https://github.com/Particular/NServiceBus/issues/2750 in the future
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_retry_immediately(TransportTransactionMode transactionMode)
        {
            var messageRedelivered = new TaskCompletionSource<bool>();

            OnTestTimeout(() => messageRedelivered.SetResult(false));

            var hasBeenCalled = false;
            var onErrorCalled = false;

            await StartPump(
                context =>
                {
                    if (hasBeenCalled)
                    {
                        messageRedelivered.SetResult(true);
                        return Task.FromResult(new MessageProcessingResult(false));
                    }

                    hasBeenCalled = true;

                    return Task.FromResult(new MessageProcessingResult(true));
                },
                context =>
                {
                    onErrorCalled = true;
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                }, transactionMode);

            await SendMessage(InputQueueName);

            Assert.True(await messageRedelivered.Task, "Should redeliver message");
            Assert.False(onErrorCalled, "Abort should not invoke on error");
        }
    }
}