namespace NServiceBus.TransportTests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using NUnit.Framework;
    using Transport;

    public class When_using_non_durable_delivery : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_on_message(TransportTransactionMode transactionMode)
        {
            RequireDeliveryConstraint<NonDurableDelivery>();

            var onMessageCalled = new TaskCompletionSource<MessageContext>();

            OnTestTimeout(() => onMessageCalled.SetCanceled());

            await StartPump((context, ct) =>
            {
                onMessageCalled.SetResult(context);
                return Task.FromResult(0);
            },
                (context, ct) => Task.FromResult(ErrorHandleResult.Handled), transactionMode);

            await SendMessage(InputQueueName, deliveryConstraints: new List<DeliveryConstraint> { new NonDurableDelivery() });

            var messageContext = await onMessageCalled.Task;

            Assert.NotNull(messageContext);
        }
    }
}