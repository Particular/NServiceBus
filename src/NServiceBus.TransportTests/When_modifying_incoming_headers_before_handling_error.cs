namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_modifying_incoming_headers_before_handling_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back_modifications(TransportTransactionMode transactionMode)
        {
            var errorHandled = new TaskCompletionSource<ErrorContext>();
            OnTestTimeout(() => errorHandled.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    context.Headers["test-header"] = "modified";
                    throw new Exception();
                },
                (context, __) =>
                {
                    errorHandled.SetResult(context);
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "test-header", "original" } });

            var errorContext = await errorHandled.Task;

            Assert.AreEqual("original", errorContext.Message.Headers["test-header"]);
        }
    }
}
