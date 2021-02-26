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
            var completed = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() => completed.SetCanceled());

            var retrying = false;
            var retried = false;
            MessageContext retryMessageContext = null;

            await StartPump(
                (context, _) =>
                {
                    if (retrying)
                    {
                        retryMessageContext = context;
                        retried = true;
                        return Task.CompletedTask;
                    }

                    context.Headers["test-header"] = "modified";
                    throw new Exception();
                },
                (context, _) =>
                {
                    retrying = true;
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                (_, __) => retried ? completed.SetCompleted() : Task.CompletedTask,
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "test-header", "original" } });

            _ = await completed.Task;

            Assert.AreEqual("original", retryMessageContext.Headers["test-header"]);
        }

        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back_header_modifications_before_handling_error(TransportTransactionMode transactionMode)
        {
            var completed = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() => completed.SetCanceled());

            ErrorContext errorContext = null;

            await StartPump(
                (context, _) =>
                {
                    context.Headers["test-header"] = "modified";
                    throw new Exception();
                },
                (context, __) =>
                {
                    errorContext = context;
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                (_, __) => completed.SetCompleted(),
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "test-header", "original" } });

            _ = await completed.Task;

            Assert.NotNull(errorContext);
            Assert.AreEqual("original", errorContext.Message.Headers["test-header"]);
        }

        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back_header_modifications_made_while_handling_error(TransportTransactionMode transactionMode)
        {
            MessageContext retryMessageContext = null;

            var completed = new TaskCompletionSource<CompleteContext>();
            OnTestTimeout(() => completed.SetCanceled());

            var retrying = false;
            var retried = false;

            await StartPump(
                (context, _) =>
                {
                    if (retrying)
                    {
                        retryMessageContext = context;
                        retried = true;
                        return Task.CompletedTask;
                    }

                    throw new Exception();
                },
                (context, __) =>
                {
                    retrying = true;
                    context.Message.Headers["test-header"] = "modified";
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                (_, __) => retried ? completed.SetCompleted() : Task.CompletedTask,
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "test-header", "original" } });

            _ = await completed.Task;

            Assert.AreEqual("original", retryMessageContext.Headers["test-header"]);
        }
    }
}
