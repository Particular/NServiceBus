namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class RecoverabilityPipelineTerminatorTests
    {
        [Test]
        public async Task Should_lock_context()
        {
            var behavior = CreateTerminator();
            var recoverabilityContext = CreateRecoverabilityContext(new ImmediateRetry(), numberOfDeliveryAttempts: 1, exceptionMessage: "test", messageId: "message-id");

            await behavior.Invoke(recoverabilityContext, _ => Task.CompletedTask);

            Assert.Throws<InvalidOperationException>(() => recoverabilityContext.RecoverabilityAction = new Discard(""));
        }

        [Test]
        public async Task When_failure_is_handled_with_immediate_retries_notification_should_be_raised()
        {
            var behavior = CreateTerminator();
            var recoverabilityContext = CreateRecoverabilityContext(new ImmediateRetry(), numberOfDeliveryAttempts: 1, exceptionMessage: "test", messageId: "message-id");

            await behavior.Invoke(recoverabilityContext, _ => Task.CompletedTask);

            var failure = messageRetriedNotifications.Single();

            Assert.AreEqual(0, failure.Attempt);
            Assert.IsTrue(failure.IsImmediateRetry);
            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_failure_is_handled_with_delayed_retries_notification_should_be_raised()
        {
            var behavior = CreateTerminator();
            var recoverabilityContext = CreateRecoverabilityContext(new DelayedRetry(TimeSpan.FromSeconds(10)), numberOfDeliveryAttempts: 1, exceptionMessage: "test", messageId: "message-id");

            await behavior.Invoke(recoverabilityContext, _ => Task.CompletedTask);

            var failure = messageRetriedNotifications.Single();

            Assert.AreEqual(1, failure.Attempt);
            Assert.IsFalse(failure.IsImmediateRetry);
            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_failure_is_handled_by_moving_to_errors_notification_should_be_raised()
        {
            var behavior = CreateTerminator();
            var recoverabilityContext = CreateRecoverabilityContext(new MoveToError(ErrorQueueAddress), exceptionMessage: "test", messageId: "message-id");

            await behavior.Invoke(recoverabilityContext, _ => Task.CompletedTask);

            var failure = messageFaultedNotifications.Single();

            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("message-id", failure.Message.MessageId);
        }

        [Test]
        public async Task When_moving_to_custom_error_queue_custom_error_queue_address_should_be_set_on_notification()
        {
            var customErrorQueueAddress = "custom-error-queue";
            var behavior = CreateTerminator();
            var recoverabilityContext = CreateRecoverabilityContext(new MoveToError(customErrorQueueAddress));

            await behavior.Invoke(recoverabilityContext, _ => Task.CompletedTask);

            var failure = messageFaultedNotifications.Single();

            Assert.IsEmpty(messageRetriedNotifications);
            Assert.AreEqual(customErrorQueueAddress, failure.ErrorQueue);
        }

        IRecoverabilityContext CreateRecoverabilityContext(
            RecoverabilityAction recoverabilityAction,
            Exception raisedException = null,
            string exceptionMessage = "default-message",
            string messageId = "default-id",
            int numberOfDeliveryAttempts = 1)
        {
            var errorContext = new ErrorContext(raisedException ?? new Exception(exceptionMessage), new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, "my-endpoint", new ContextBag());
            return new RecoverabilityContext(errorContext, null, new Dictionary<string, string>(), recoverabilityAction, new FakeRootContext());
        }

        RecoverabilityPipelineTerminator CreateTerminator()
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

            return new RecoverabilityPipelineTerminator(
                messageRetryNotification,
                messageFaultedNotification);
        }

        List<MessageToBeRetried> messageRetriedNotifications;
        List<MessageFaulted> messageFaultedNotifications;

        static string ErrorQueueAddress = "error-queue";


        class FakeRootContext : IBehaviorContext
        {
            public FakeRootContext()
            {
                Extensions = new ContextBag();
                Extensions.Set<IPipelineCache>(new FakePipelineCache());
            }

            public IServiceProvider Builder => throw new NotImplementedException();

            public CancellationToken CancellationToken => CancellationToken.None;

            public ContextBag Extensions { get; }
        }

        class FakePipelineCache : IPipelineCache
        {
            public FakePipelineCache()
            {
            }

            public IPipeline<TContext> Pipeline<TContext>() where TContext : IBehaviorContext
            {
                return new FakePipeline<TContext>();
            }

            public class FakePipeline<TContext> : IPipeline<TContext> where TContext : IBehaviorContext
            {
                public Task Invoke(TContext context)
                {
                    return Task.CompletedTask;
                }
            }
        }

    }
}