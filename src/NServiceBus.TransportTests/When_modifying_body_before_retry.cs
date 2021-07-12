namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_modifying_body_before_retry : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back(TransportTransactionMode transactionMode)
        {
            var originalBody = new ReadOnlyCollection<byte>(Encoding.UTF8.GetBytes("Hello World!"));
            var retried = CreateTaskCompletionSource<MessageContext>();

            var retrying = false;

            await StartPump(
                (context, _) =>
                {
                    if (retrying)
                    {
                        return retried.SetCompleted(context);
                    }

                    context.Body[0] = 0;
                    throw new Exception();
                },
                (context, _) =>
                {
                    retrying = true;
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                transactionMode);

            await SendMessage(InputQueueName, body: originalBody.ToArray());

            var retryMessageContext = await retried.Task;

            Assert.AreEqual(originalBody, retryMessageContext.Body);
        }
    }
}