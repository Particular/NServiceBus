namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transports;

    public class When_on_message_throws : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_call_on_error(TransportTransactionMode transactionMode)
        {
            var onErrorCalled = new TaskCompletionSource<ErrorContext>();
            var cts = new CancellationTokenSource();

            cts.CancelAfter(TimeSpan.FromSeconds(10));
            cts.Token.Register(() => onErrorCalled.SetCanceled());

            await StartPump(async context =>
            {
                await Console.Out.WriteLineAsync($"Onmessage({context.MessageId})");

                throw new Exception("Simulated exception");
            },
                async context =>
                {
                    await Console.Out.WriteLineAsync($"OnError({context.MessageId}) - {context.NumberOfProcessingAttempts}");
                    onErrorCalled.SetResult(context);

                    return false;
                }, transactionMode);

            await SendMessage(InputQueueName);

            var errorContext = await onErrorCalled.Task;

            Assert.AreEqual(errorContext.Exception.Message, "Simulated exception");

            Assert.AreEqual(1, errorContext.NumberOfProcessingAttempts);
        }
    }
}