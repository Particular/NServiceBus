namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class SatelliteRecoverabilityExecutorTests
    {
        [SetUp]
        public void SetUp()
        {
            dispatcher = new FakeDispatcher();
        }

        [Test]
        public async Task When_delayed_retries_not_supported_but_policy_demands_it_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(delayedRetriesSupported: false);
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher, RecoverabilityAction.DelayedRetry(TimeSpan.FromDays(1)));

            Assert.True(dispatcher.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_immediate_retries_not_supported_but_policy_demands_it_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(immediateRetriesSupported: false);
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher, RecoverabilityAction.ImmediateRetry());

            Assert.True(dispatcher.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_unsupported_action_returned_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor();
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher, new UnsupportedAction());

            Assert.True(dispatcher.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_discard_action_returned_should_discard_message()
        {
            var recoverabilityExecutor = CreateExecutor();
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher, RecoverabilityAction.Discard("not needed anymore"));

            Assert.False(dispatcher.MessageWasSent());
        }

        [Test]
        public async Task When_moving_to_custom_error_queue_custom_error_queue_address_should_send_to_address()
        {
            var customErrorQueueAddress = "custom-error-queue";
            var recoverabilityExecutor = CreateExecutor();
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher, RecoverabilityAction.MoveToError(customErrorQueueAddress));

            Assert.True(dispatcher.MessageWasSentTo(customErrorQueueAddress));
        }

        static ErrorContext CreateErrorContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", int numberOfDeliveryAttempts = 1)
        {
            return new ErrorContext(raisedException ?? new Exception(exceptionMessage), new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, "my-endpoint", new ContextBag());
        }

        SatelliteRecoverabilityExecutor CreateExecutor(bool delayedRetriesSupported = true, bool immediateRetriesSupported = true)
        {
            return new SatelliteRecoverabilityExecutor(
                immediateRetriesSupported,
                delayedRetriesSupported,
                new RecoverabilityConfig(new ImmediateConfig(0), new DelayedConfig(0, TimeSpan.Zero), new FailedConfig(ErrorQueueAddress, new HashSet<Type>())),
                delayedRetriesSupported ? new DelayedRetryExecutor() : null,
                new MoveToErrorsExecutor(new Dictionary<string, string>(), headers => { }));
        }

        FakeDispatcher dispatcher;

        static string ErrorQueueAddress = "error-queue";

        class UnsupportedAction : RecoverabilityAction
        {
            public override ErrorHandleResult ErrorHandleResult => throw new NotImplementedException();
        }

        class FakeDispatcher : IMessageDispatcher
        {
            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
            {
                Assert.False(outgoingMessages.MulticastTransportOperations.Any(), "Multicast should never happen");
                Assert.IsNull(transportOperation, "Only a single dispatch is supported");
                transportOperation = outgoingMessages.UnicastTransportOperations.Single();
                return Task.CompletedTask;
            }

            public bool MessageWasSentTo(string address)
            {
                return transportOperation.Destination == address;
            }

            public bool MessageWasSent()
            {
                return transportOperation != null;
            }

            UnicastTransportOperation transportOperation;
        }
    }
}