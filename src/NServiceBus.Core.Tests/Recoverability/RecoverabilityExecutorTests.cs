namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
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
        public async Task When_failure_is_handled_with_immediate_retries_notification_should_be_raised()
        {
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysRetry());
            var recoverabilityContext = CreateRecoverabilityContext(numberOfDeliveryAttempts: 1, exceptionMessage: "test", messageId: "message-id");

            await recoverabilityExecutor.Invoke(recoverabilityContext);

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
            var recoverabilityContext = CreateRecoverabilityContext(numberOfDeliveryAttempts: 1, exceptionMessage: "test", messageId: "message-id");

            await recoverabilityExecutor.Invoke(recoverabilityContext);

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
            var recoverabilityContext = CreateRecoverabilityContext(exceptionMessage: "test", messageId: "message-id");

            await recoverabilityExecutor.Invoke(recoverabilityContext);

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
            var recoverabilityContext = CreateRecoverabilityContext(messageId: "message-id");

            await recoverabilityExecutor.Invoke(recoverabilityContext);

            Assert.True(dispatchCollector.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_immediate_retries_not_supported_but_policy_demands_it_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.AlwaysRetry(),
                immediateRetriesSupported: false);
            var recoverabilityContext = CreateRecoverabilityContext(messageId: "message-id");

            await recoverabilityExecutor.Invoke(recoverabilityContext);

            Assert.True(dispatchCollector.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_unsupported_action_returned_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.Unsupported());
            var recoverabilityContext = CreateRecoverabilityContext(messageId: "message-id");

            await recoverabilityExecutor.Invoke(recoverabilityContext);

            Assert.True(dispatchCollector.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_discard_action_returned_should_discard_message()
        {
            var recoverabilityExecutor = CreateExecutor(
                RetryPolicy.Discard("not needed anymore"));
            var recoverabilityContext = CreateRecoverabilityContext(messageId: "message-id");

            var result = await recoverabilityExecutor.Invoke(recoverabilityContext);

            Assert.AreEqual(ErrorHandleResult.Handled, result);
            Assert.IsEmpty(messageRetriedNotifications);
            Assert.IsEmpty(messageFaultedNotifications);
            Assert.True(dispatchCollector.NoMessageWasSent());
        }

        [Test]
        public async Task When_moving_to_custom_error_queue_custom_error_queue_address_should_be_set_on_notification()
        {
            var customErrorQueueAddress = "custom-error-queue";
            var recoverabilityExecutor = CreateExecutor(RetryPolicy.AlwaysMoveToErrors(customErrorQueueAddress));
            var recoverabilityContext = CreateRecoverabilityContext();

            await recoverabilityExecutor.Invoke(recoverabilityContext);

            var failure = messageFaultedNotifications.Single();

            Assert.IsEmpty(messageRetriedNotifications);
            Assert.AreEqual(customErrorQueueAddress, failure.ErrorQueue);
        }

        IRecoverabilityContext CreateRecoverabilityContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", int numberOfDeliveryAttempts = 1)
        {
            var errorContext = new ErrorContext(raisedException ?? new Exception(exceptionMessage), new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, "my-endpoint", new ContextBag());
            return new RecoverabilityContext(errorContext, new FakeRootContext(dispatchCollector));
        }

        RecoverabilityExecutor CreateExecutor(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy, bool delayedRetriesSupported = true, bool immediateRetriesSupported = true)
        {
            messageRetriedNotifications = new List<MessageToBeRetried>();
            var messageRetryNotification = new Notification<MessageToBeRetried>();
            messageRetryNotification.Subscribe((e, _) =>
            {
                messageRetriedNotifications.Add(e);
                return Task.FromResult(0);
            });

            messageFaultedNotifications = new List<MessageFaulted>();
            var messageFaultedNotification = new Notification<MessageFaulted>();
            messageFaultedNotification.Subscribe((e, _) =>
            {
                messageFaultedNotifications.Add(e);
                return Task.FromResult(0);
            });

            return new RecoverabilityExecutor(
                immediateRetriesSupported,
                delayedRetriesSupported,
                policy,
                new RecoverabilityConfig(new ImmediateConfig(0), new DelayedConfig(0, TimeSpan.Zero), new FailedConfig(ErrorQueueAddress, new HashSet<Type>())),
                delayedRetriesSupported ? new DelayedRetryExecutor() : null,
                new MoveToErrorsExecutor(new Dictionary<string, string>(), headers => { }),
                messageRetryNotification,
                messageFaultedNotification);
        }

        DispatchCollector dispatchCollector;
        List<MessageToBeRetried> messageRetriedNotifications;
        List<MessageFaulted> messageFaultedNotifications;

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

        class FakeRootContext : IBehaviorContext
        {
            public FakeRootContext(DispatchCollector dispatchCollector)
            {
                Extensions = new ContextBag();

                Extensions.Set<IPipelineCache>(new FakePipelineCache(dispatchCollector));
            }

            public IServiceProvider Builder => throw new NotImplementedException();

            public CancellationToken CancellationToken => CancellationToken.None;

            public ContextBag Extensions { get; }
        }

        class FakePipelineCache : IPipelineCache
        {
            public FakePipelineCache(DispatchCollector dispatchCollector)
            {
                this.dispatchCollector = dispatchCollector;
            }

            public IPipeline<TContext> Pipeline<TContext>() where TContext : IBehaviorContext
            {
                return (IPipeline<TContext>)new FakeDispatchPipeline(dispatchCollector);
            }

            readonly DispatchCollector dispatchCollector;
        }

        class FakeDispatchPipeline : IPipeline<IDispatchContext>
        {
            public FakeDispatchPipeline(DispatchCollector dispatchCollector)
            {
                this.dispatchCollector = dispatchCollector;
            }

            public Task Invoke(IDispatchContext context)
            {
                dispatchCollector.Collect(context.Operations.Single());

                return Task.CompletedTask;
            }

            DispatchCollector dispatchCollector;
        }
    }
}