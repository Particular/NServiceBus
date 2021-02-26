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
            var completed = new TaskCompletionSource<CompleteContext>();
            OnTestTimeout(() => completed.SetCanceled());

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
                (context, _) => completed.SetCompleted(context),
                transactionMode);

            await SendMessage(InputQueueName);

            var completeContext = await completed.Task;

            Assert.AreEqual("MyOnMessageValue", completeContext.Extensions.Get<string>("MyOnMessageKey"));
            Assert.AreEqual("MyOnErrorValue", completeContext.Extensions.Get<string>("MyOnErrorKey"));
        }
    }
}
