namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class RecoverabilityExecutorTests
    {
        [SetUp]
        public void SetUp()
        {
            dispatcher = new FakeDispatcher();
            eventAggregator = new FakeEventAggregator();
        }

        [Test]
        public async Task When_notification_turned_off_no_notification_should_be_raised()
        {
            var policy = RetryPolicy.Return(
                actions: new[]
                {
                    RecoverabilityAction.ImmediateRetry(),
                    RecoverabilityAction.DelayedRetry(TimeSpan.FromSeconds(10)),
                    RecoverabilityAction.MoveToError()
                });
            var executor = CreateExecutor(policy, raiseNotifications: false);
            var errorContext = CreateErrorContext();

            await executor.Invoke(errorContext); //force retry
            await executor.Invoke(errorContext); //force delayed retry
            await executor.Invoke(errorContext); //force move to errors

            Assert.IsNull(eventAggregator.GetNotification<MessageFaulted>());
            Assert.IsNull(eventAggregator.GetNotification<MessageToBeRetried>());
        }

        [Test]
        public async Task When_failure_is_caused_by_deserialization_exception_no_retries_should_be_performed()
        {
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysRetry());
            var errorContext = CreateErrorContext(new MessageDeserializationException(""));

            var errorHandleResult = await recoverabilityExecutor.Invoke(errorContext);

            Assert.AreEqual(ErrorHandleResult.Handled, errorHandleResult, "Deserialization exception should cause immediate send to error queue");
            Assert.AreEqual(1, dispatcher.TransportOperations.UnicastTransportOperations.Count);
            Assert.AreEqual(ErrorQueueAddress, dispatcher.TransportOperations.UnicastTransportOperations.First().Destination);
        }

        [Test]
        public async Task When_running_with_no_transactions_no_retries_should_be_performed()
        {
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysRetry(), transactionsOn: false);
            var errorContext = CreateErrorContext();

            var errorHandleResult = await recoverabilityExecutor.Invoke(errorContext);

            Assert.AreEqual(ErrorHandleResult.Handled, errorHandleResult, "Transactions disabled should cause immediate send to error queue");
            Assert.AreEqual(1, dispatcher.TransportOperations.UnicastTransportOperations.Count);
            Assert.AreEqual(ErrorQueueAddress, dispatcher.TransportOperations.UnicastTransportOperations.First().Destination);
        }

        [Test]
        public async Task When_failure_is_handled_with_immediate_retries_notification_should_be_raised()
        {
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysRetry());
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 1, exceptionMessage: "test", messageId: "message-id");

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = eventAggregator.GetNotification<MessageToBeRetried>();

            Assert.AreEqual(0, failure.Attempt);
            Assert.IsTrue(failure.IsImmediateRetry);
            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_failure_is_handled_with_delayed_retries_notification_should_be_raised()
        {
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysDelay(TimeSpan.FromSeconds(10)));
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 1, exceptionMessage: "test", messageId: "message-id");

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = eventAggregator.GetNotification<MessageToBeRetried>();

            Assert.AreEqual(1, failure.Attempt);
            Assert.IsFalse(failure.IsImmediateRetry);
            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_failure_is_handled_by_moving_to_errors_notification_should_be_raised()
        {
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysMoveToErrors());
            var errorContext = CreateErrorContext(exceptionMessage: "test", messageId: "message-id");

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = eventAggregator.GetNotification<MessageFaulted>();

            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_delayed_retries_not_supported_but_policy_demands_it_should_move_to_error()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.AlwaysDelay(TimeSpan.FromDays(1)),
                transactionsOn: true,
                delayedRetriesSupported: false);
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = eventAggregator.GetNotification<MessageFaulted>();
            var retried = eventAggregator.GetNotification<MessageToBeRetried>();

            Assert.IsNotNull(failure);
            Assert.IsNull(retried);
        }

        static ErrorContext CreateErrorContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", int numberOfDeliveryAttempts = 1)
        {
            return new ErrorContext(raisedException ?? new Exception(exceptionMessage), new Dictionary<string, string>(), messageId, Stream.Null, new TransportTransaction(), numberOfDeliveryAttempts);
        }

        RecoverabilityExecutor CreateExecutor(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy, bool transactionsOn = true, bool delayedRetriesSupported = true, bool raiseNotifications = true)
        {
            return new RecoverabilityExecutor(
                raiseNotifications,
                policy,
                new RecoverabilityConfig(new ImmediateConfig(), new DelayedConfig(0, TimeSpan.MinValue, delayedRetriesSupported)),
                eventAggregator,
                delayedRetriesSupported ? new DelayedRetryExecutor(InputQueueAddress, dispatcher) : null,
                new MoveToErrorsExecutor(dispatcher, ErrorQueueAddress, new Dictionary<string, string>()),
                transactionsOn);
        }

        FakeDispatcher dispatcher;
        FakeEventAggregator eventAggregator;

        static string ErrorQueueAddress = "error-queue";
        static string InputQueueAddress = "input-queue";

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
                return new RetryPolicy(new [] { RecoverabilityAction.DelayedRetry(delay) }).Invoke;
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> AlwaysMoveToErrors()
            {
                return new RetryPolicy(new[] { RecoverabilityAction.MoveToError() }).Invoke;
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> AlwaysRetry()
            {
                return new RetryPolicy(new[] { RecoverabilityAction.ImmediateRetry() }).Invoke;
            }

            public static Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> Return(RecoverabilityAction[] actions)
            {
                return new RetryPolicy(actions).Invoke;
            }

            Queue<RecoverabilityAction> actions;
        }

        class FakeDispatcher : IDispatchMessages
        {
            public TransportOperations TransportOperations { get; private set; }

            public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
            {
                TransportOperations = outgoingMessages;
                return TaskEx.CompletedTask;
            }
        }
    }
}