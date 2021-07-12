namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    // Failing
    public class When_modifying_body_before_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back(TransportTransactionMode transactionMode)
        {
            var originalBody = new ReadOnlyCollection<byte>(Encoding.UTF8.GetBytes("Hello World!"));
            var errorHandled = CreateTaskCompletionSource<ErrorContext>();
            await StartPump(
                (context, _) =>
                {
                    context.Body[0] = 0;
                    throw new Exception();
                },
                (context, __) =>
                {
                    errorHandled.SetResult(context);
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName, body: originalBody.ToArray());

            var errorContext = await errorHandled.Task;

            Assert.AreEqual(originalBody, errorContext.Message.Body);
        }
    }
}