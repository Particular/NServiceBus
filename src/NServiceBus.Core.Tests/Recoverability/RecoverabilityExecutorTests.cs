using System.Threading;
using NServiceBus.Transport;

namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
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
        }

        [Test]
        public async Task When_notification_turned_off_no_notification_should_be_raised()
        {
            var policy = RetryPolicy.Return(
                actions: new RecoverabilityAction[]
                {
                    RecoverabilityAction.ImmediateRetry(),
                    RecoverabilityAction.DelayedRetry(TimeSpan.FromSeconds(10)),
                    RecoverabilityAction.MoveToError("errorQueue")
                });
            var executor = CreateExecutor(policy, raiseNotifications: false);
            var errorContext = CreateErrorContext();

            await executor.Invoke(errorContext); //force retry
            await executor.Invoke(errorContext); //force delayed retry
            await executor.Invoke(errorContext); //force move to errors

            Assert.IsEmpty(messageRetriedNotifications);
            Assert.IsEmpty(messageFaultedNotifications);
        }

        [Test]
        public async Task When_failure_is_handled_with_immediate_retries_notification_should_be_raised()
        {
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysRetry());
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 1, exceptionMessage: "test", messageId: "message-id");

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = messageRetriedNotifications.Single();

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

            var failure = messageRetriedNotifications.Single();

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

            var failure = messageFaultedNotifications.Single();

            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_delayed_retries_not_supported_but_policy_demands_it_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.AlwaysDelay(TimeSpan.FromDays(1)),
                delayedRetriesSupported: false);
            var errorContext = CreateErrorContext(messageId: "message-id");

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = messageFaultedNotifications.Single();

            Assert.IsEmpty(messageRetriedNotifications);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_immediate_retries_not_supported_but_policy_demands_it_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.AlwaysRetry(),
                immediateRetriesSupported: false);
            var errorContext = CreateErrorContext(messageId: "message-id");

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = messageFaultedNotifications.Single();

            Assert.IsEmpty(messageRetriedNotifications);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_unsupported_action_returned_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.Unsupported());
            var errorContext = CreateErrorContext(messageId: "message-id");

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = messageFaultedNotifications.Single();

            Assert.IsEmpty(messageRetriedNotifications);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_discard_action_returned_should_discard_message()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.Discard("not needed anymore"));
            var errorContext = CreateErrorContext(messageId: "message-id");

            var result = await recoverabilityExecutor.Invoke(errorContext);

            Assert.AreEqual(ErrorHandleResult.Handled, result);
            Assert.IsEmpty(messageRetriedNotifications);
            Assert.IsEmpty(messageFaultedNotifications);
        }

        [Test]
        public async Task When_moving_to_custom_error_queue_custom_error_queue_address_should_be_set_on_notification()
        {
            var customErrorQueueAddress = "custom-error-queue";
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysMoveToErrors(customErrorQueueAddress));
            var errorContext = CreateErrorContext();

            await recoverabilityExecutor.Invoke(errorContext);

            var failure = messageFaultedNotifications.Single();

            Assert.IsEmpty(messageRetriedNotifications);
            Assert.AreEqual(customErrorQueueAddress, failure.ErrorQueue);
        }

        static ErrorContext CreateErrorContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", int numberOfDeliveryAttempts = 1)
        {
            return new ErrorContext(raisedException ?? new Exception(exceptionMessage), new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts);
        }

        RecoverabilityExecutor CreateExecutor(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy, bool delayedRetriesSupported = true, bool immediateRetriesSupported = true, bool raiseNotifications = true)
        {
            messageRetriedNotifications = new List<MessageToBeRetried>();
            var messageRetryNotification = new Notification<MessageToBeRetried>();
            messageRetryNotification.Subscribe(e =>
            {
                messageRetriedNotifications.Add(e);
                return Task.FromResult(0);
            });

            messageFaultedNotifications = new List<MessageFaulted>();
            var messageFaultedNotification = new Notification<MessageFaulted>();
            messageFaultedNotification.Subscribe(e =>
            {
                messageFaultedNotifications.Add(e);
                return Task.FromResult(0);
            });

            return new RecoverabilityExecutor(
                raiseNotifications,
                immediateRetriesSupported,
                delayedRetriesSupported,
                policy,
                new RecoverabilityConfig(new ImmediateConfig(0), new DelayedConfig(0, TimeSpan.Zero), new FailedConfig(ErrorQueueAddress, new HashSet<Type>())),
                delayedRetriesSupported ? new DelayedRetryExecutor(InputQueueAddress, dispatcher) : null,
                new MoveToErrorsExecutor(dispatcher, new Dictionary<string, string>(), headers => { }),
                messageRetryNotification,
                messageFaultedNotification);
        }

        FakeDispatcher dispatcher;

        List<MessageToBeRetried> messageRetriedNotifications;
        List<MessageFaulted> messageFaultedNotifications;

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
            public TransportOperations TransportOperations { get; private set; }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction)
            {
                TransportOperations = outgoingMessages;
                return Task.CompletedTask;
            }
        }
    }
}