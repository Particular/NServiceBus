namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class RecoverabilityExecutorTests
    {
        [SetUp]
        public void SetUp()
        {
            dispatchCollector = new DispatchCollector();
        }

        [Test]
        public async Task When_unsupported_action_returned_should_move_to_errors()
        {
            await InvokeExecutor(new UnsupportedAction());

            Assert.True(dispatchCollector.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_discard_action_returned_should_discard_message()
        {
            await InvokeExecutor(new Discard("not needed anymore"));

            Assert.True(dispatchCollector.NoMessageWasSent());
        }

        [Test]
        public async Task When_moving_to_custom_error_queue_custom_error_queue_address_should_send_to_address()
        {
            var customErrorQueueAddress = "custom-error";

            await InvokeExecutor(RecoverabilityAction.MoveToError(customErrorQueueAddress));

            Assert.True(dispatchCollector.MessageWasSentTo(customErrorQueueAddress));
        }

        Task InvokeExecutor(RecoverabilityAction recoverabilityAction,
            Exception raisedException = null,
            string exceptionMessage = "default-message",
            string messageId = "default-id",
            int numberOfDeliveryAttempts = 1,
            CancellationToken cancellationToken = default)
        {
            var errorContext = new ErrorContext(raisedException ?? new Exception(exceptionMessage), new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, "my-endpoint", new ContextBag());

            var executor = new RecoverabilityExecutor(
                new DelayedRetryExecutor(),
                new MoveToErrorsExecutor(new Dictionary<string, string>(), headers => { }));

            return executor.Invoke(
                errorContext,
                recoverabilityAction,
                 (transportOperation, _) =>
                 {
                     dispatchCollector.Collect(transportOperation);
                     return Task.CompletedTask;
                 }
                 , cancellationToken);
        }

        DispatchCollector dispatchCollector;

        static string ErrorQueueAddress = "error-queue";

        class UnsupportedAction : RecoverabilityAction
        {
            public override ErrorHandleResult ErrorHandleResult => throw new NotImplementedException();
        }

        class DispatchCollector
        {
            string targetAddress;

            public IDictionary<string, string> MessageHeaders { get; private set; }

            public void Collect(TransportOperation transportOperation)
            {
                var unicastAddressTag = transportOperation.AddressTag as UnicastAddressTag;

                Assert.IsNotNull(unicastAddressTag);

                targetAddress = unicastAddressTag.Destination;

                MessageHeaders = transportOperation.Message.Headers;
            }

            public bool MessageWasSentTo(string address)
            {
                return address == targetAddress;
            }

            public bool NoMessageWasSent()
            {
                return targetAddress == null;
            }
        }
    }
}