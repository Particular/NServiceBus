namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_modifying_incoming_headers : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back_header_modifications_between_processing_attempts(TransportTransactionMode transactionMode)
        {
            var retried = new TaskCompletionSource<MessageContext>();
            OnTestTimeout(() => retried.SetCanceled());

            var retrying = false;

            await StartPump(
                (context, _) =>
                {
                    if (retrying)
                    {
                        return retried.SetCompleted(context);
                    }

                    context.Headers["test-header"] = "modified";
                    throw new Exception();
                },
                (context, _) =>
                {
                    retrying = true;
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "test-header", "original" } });

            var retryMessageContext = await retried.Task;

            Assert.AreEqual("original", retryMessageContext.Headers["test-header"]);
        }

        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back_header_modifications_before_handling_error(TransportTransactionMode transactionMode)
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

        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back_header_modifications_made_while_handling_error(TransportTransactionMode transactionMode)
        {
            var retried = new TaskCompletionSource<MessageContext>();
            OnTestTimeout(() => retried.SetCanceled());

            var retrying = false;

            await StartPump(
                (context, _) => retrying ? retried.SetCompleted(context) : throw new Exception(),
                (context, _) =>
                {
                    retrying = true;
                    context.Message.Headers["test-header"] = "modified";
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "test-header", "original" } });

            var retryMessageContext = await retried.Task;

            Assert.AreEqual("original", retryMessageContext.Headers["test-header"]);
        }
    }
}
