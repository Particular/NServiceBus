namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MoveFaultsToErrorQueueHandlerTests
    {
        FakeCriticalError criticalError;

        [SetUp]
        public void Setup()
        {
            criticalError = new FakeCriticalError();
        }

        [Test]
        public async Task ShouldMovePreviouslyFailedMessageToErrorQueue()
        {
            var fakeDispatcher = new FakeDispatcher();
            var behavior = CreateBehavior(dispatcher: fakeDispatcher);
            var context = CreateContext("some-id");

            behavior.MarkForFutureHandling(context, new Exception());

            await behavior.HandleIfPreviouslyFailed(context);

            Assert.AreEqual("some-id", fakeDispatcher.ErrorOperation.Message.MessageId);
            Assert.AreEqual("error", fakeDispatcher.ErrorOperation.Destination);
        }

        [Test]
        public void ShouldRaiseCriticalErrorWhenMovingToErrorQueueFails()
        {
            var fakeDispatcher = new FakeDispatcher
            {
                ThrowOnDispatch = true
            };

            var behavior = CreateBehavior(dispatcher: fakeDispatcher);
            var context = CreateContext();

            var behaviorInvocation = new AsyncTestDelegate(async () =>
            {
                await behavior.MoveMessageToErrorQueue(context, new Exception());
            });

            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.That(behaviorInvocation, Throws.InstanceOf<Exception>());
            Assert.True(criticalError.ErrorRaised);
        }

        [Test]
        public async Task ShouldRaiseNotificationWhenMessageIsMovedToErrorQueue()
        {
            var behavior = CreateBehavior();
            var eventAggregator = new FakeEventAggregator();
            var context = CreateContext("some-id", eventAggregator);

            await behavior.MoveMessageToErrorQueue(context, new Exception("exception-message"));

            var notification = eventAggregator.GetNotification<MessageFaulted>();

            Assert.AreEqual("some-id", notification.Message.MessageId);
            Assert.AreEqual("exception-message", notification.Exception.Message);
        }

        [Test]
        public async Task ShouldHandleMessageAfterMarkingAsFailed()
        {
            var behavior = CreateBehavior();
            var context = CreateContext();

            behavior.MarkForFutureHandling(context, new Exception());

            var messageHandeled = await behavior.HandleIfPreviouslyFailed(context);

            Assert.IsTrue(messageHandeled, "Message should be handled after being marked as failed.");
        }

        MoveFaultsToErrorQueueHandler CreateBehavior(Dictionary<string, string> staticFaultMetadata = null, IDispatchMessages dispatcher = null)
        {
            if (dispatcher == null)
            {
                dispatcher = new FakeDispatcher();
            }

            var behavior = new MoveFaultsToErrorQueueHandler(
                criticalError,
                new FailureInfoStorage(10),
                new MoveToErrorsActionExecutor(dispatcher, "error", staticFaultMetadata ?? new Dictionary<string, string>()));

            return behavior;
        }

        static TestableTransportReceiveContext CreateContext(string messageId = "message-id", FakeEventAggregator eventAggregator = null)
        {
            var context = new TestableTransportReceiveContext
            {
                Message = new IncomingMessage(messageId, new Dictionary<string, string>(), Stream.Null)
            };

            context.Extensions.Set<IEventAggregator>(eventAggregator ?? new FakeEventAggregator());

            return context;
        }

        class FakeDispatcher : IDispatchMessages
        {
            public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
            {
                if (ThrowOnDispatch)
                {
                    throw new Exception("Failed to send to error queue");
                }

                ErrorOperation = outgoingMessages.UnicastTransportOperations.First();

                return Task.FromResult(0);
            }

            public UnicastTransportOperation ErrorOperation { get; private set; }
            public bool ThrowOnDispatch { get; set; }
        }

        class FakeCriticalError : CriticalError
        {
            public FakeCriticalError() : base(_ => TaskEx.CompletedTask)
            {
            }

            public bool ErrorRaised { get; private set; }

            public override void Raise(string errorMessage, Exception exception)
            {
                ErrorRaised = true;
            }
        }
    }
}