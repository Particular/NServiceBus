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
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.AlwaysDelay(TimeSpan.FromDays(1)),
                delayedRetriesSupported: false);
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher);

            Assert.True(dispatcher.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_immediate_retries_not_supported_but_policy_demands_it_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.AlwaysRetry(),
                immediateRetriesSupported: false);
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher);

            Assert.True(dispatcher.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_unsupported_action_returned_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.Unsupported());
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher);

            Assert.True(dispatcher.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_discard_action_returned_should_discard_message()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.Discard("not needed anymore"));
            var errorContext = CreateErrorContext();

            var result = await recoverabilityExecutor.Invoke(errorContext, dispatcher);

            Assert.AreEqual(ErrorHandleResult.Handled, result);
            Assert.False(dispatcher.MessageWasSent());
        }

        [Test]
        public async Task When_moving_to_custom_error_queue_custom_error_queue_address_should_send_to_address()
        {
            var customErrorQueueAddress = "custom-error-queue";
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysMoveToErrors(customErrorQueueAddress));
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext, dispatcher);

            Assert.True(dispatcher.MessageWasSentTo(customErrorQueueAddress));
        }

        static ErrorContext CreateErrorContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", int numberOfDeliveryAttempts = 1)
        {
            return new ErrorContext(raisedException ?? new Exception(exceptionMessage), new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, "my-endpoint", new ContextBag());
        }

        SatelliteRecoverabilityExecutor CreateExecutor(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy, bool delayedRetriesSupported = true, bool immediateRetriesSupported = true)
        {
            return new SatelliteRecoverabilityExecutor(
                immediateRetriesSupported,
                delayedRetriesSupported,
                policy,
                new RecoverabilityConfig(new ImmediateConfig(0), new DelayedConfig(0, TimeSpan.Zero), new FailedConfig(ErrorQueueAddress, new HashSet<Type>())),
                delayedRetriesSupported ? new DelayedRetryExecutor() : null,
                new MoveToErrorsExecutor(new Dictionary<string, string>(), headers => { }));
        }

        FakeDispatcher dispatcher;

        static string ErrorQueueAddress = "error-queue";

        class RetryPolicy
        {
            RetryPolicy(RecoverabilityAction[] actions)
            {
                this.actions = new Queue<RecoverabilityAction>(actions);
            }

            public RecoverabilityAction Invoke(RecoverabilityConfig config, ErrorContext errorContext)
            {
                return actions.Dequeue();
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> AlwaysDelay(TimeSpan delay)
            {
                return new RetryPolicy(new[]
                {
                    RecoverabilityAction.DelayedRetry(delay)
                }).Invoke;
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> AlwaysMoveToErrors(string errorQueueAddress = "errorQueue")
            {
                return new RetryPolicy(new[]
                {
                    RecoverabilityAction.MoveToError(errorQueueAddress)
                }).Invoke;
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> AlwaysRetry()
            {
                return new RetryPolicy(new[]
                {
                    RecoverabilityAction.ImmediateRetry()
                }).Invoke;
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> Return(RecoverabilityAction[] actions)
            {
                return new RetryPolicy(actions).Invoke;
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> Unsupported()
            {
                return new RetryPolicy(new[]
                {
                    new UnsupportedAction()
                }).Invoke;
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> Discard(string reason)
            {
                return new RetryPolicy(new[]
                {
                    new Discard(reason),
                }).Invoke;
            }

            Queue<RecoverabilityAction> actions;
        }

        class UnsupportedAction : RecoverabilityAction
        {
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