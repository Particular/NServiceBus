namespace NServiceBus.TransportTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_starting_again : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_complete(TransportTransactionMode transactionMode)
        {
            bool messageReceived = false;
            var messageReceivedCompletionSource = CreateTaskCompletionSource();

            await StartPump(
                (_, __) =>
                {
                    messageReceived = true;
                    messageReceivedCompletionSource.SetResult();
                    return Task.CompletedTask;
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);

            await receiver.StopReceive();

            await SendMessage(InputQueueName);

            await receiver.StartReceive();

            await messageReceivedCompletionSource.Task;

            Assert.That(messageReceived, Is.True);
        }
    }
}
