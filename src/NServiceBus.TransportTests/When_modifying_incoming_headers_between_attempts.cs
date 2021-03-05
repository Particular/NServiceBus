﻿namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_modifying_incoming_headers_between_attempts : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back_modifications(TransportTransactionMode transactionMode)
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
    }
}
