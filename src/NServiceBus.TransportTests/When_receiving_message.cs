namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_receiving_message : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_float_context(TransportTransactionMode transactionMode)
        {
            var onComplete = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() => onComplete.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    context.Extensions.Set("MyOnMessageKey", "MyOnMessageValue");

                    throw new Exception("Simulated exception");
                },
                (context, _) =>
                {
                    context.Extensions.Set("MyOnErrorKey", "MyOnErrorValue");

                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode,
                onComplete: (context, _) =>
                {
                    onComplete.SetResult(context);

                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            var completeContext = await onComplete.Task;

            Assert.AreEqual("MyOnMessageValue", completeContext.Extensions.Get<string>("MyOnMessageKey"));
            Assert.AreEqual("MyOnErrorValue", completeContext.Extensions.Get<string>("MyOnErrorKey"));
        }
    }
}
