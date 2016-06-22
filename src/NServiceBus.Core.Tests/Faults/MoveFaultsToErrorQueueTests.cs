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
    public class MoveFaultsToErrorQueueTests
    {
        FakeCriticalError criticalError;

        [SetUp]
        public void Setup()
        {
            criticalError = new FakeCriticalError();
        }

        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task ShouldForwardToErrorQueueForAllExceptions(TransportTransactionMode transactionMode)
        {
            var fakeDispatcher = new FakeDispatcher();
            var behavior = CreateBehavior(transactionMode, dispatcher: fakeDispatcher);
            var context = CreateContext("some-id");

            await behavior.Invoke(context, () => { throw new Exception(); });

            if (transactionMode != TransportTransactionMode.None)
            {
                await behavior.Invoke(context, () => Task.FromResult(0));
            }

            Assert.AreEqual("some-id", fakeDispatcher.ErrorOperation.Message.MessageId);
            Assert.AreEqual("error", fakeDispatcher.ErrorOperation.Destination);
        }

        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public void ShouldInvokeCriticalErrorIfForwardingFails(TransportTransactionMode transactionMode)
        {
            var fakeDispatcher = new FakeDispatcher
            {
                ThrowOnDispatch = true
            };

            var behavior = CreateBehavior(transactionMode, dispatcher: fakeDispatcher);
            var context = CreateContext();

            var behaviorInvocation = new AsyncTestDelegate(async () =>
            {
                await behavior.Invoke(context, () => { throw new Exception(); });

                if (transactionMode != TransportTransactionMode.None)
                {
                    await behavior.Invoke(context, () => Task.FromResult(0));
                }
            });

            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.That(behaviorInvocation, Throws.InstanceOf<Exception>());
            Assert.True(criticalError.ErrorRaised);
        }

        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task ShouldRegisterFailureInfoWhenMessageIsForwarded(TransportTransactionMode transactionMode)
        {
            var behavior = CreateBehavior(transactionMode);
            var eventAggregator = new FakeEventAggregator();
            var context = CreateContext("some-id", eventAggregator);

            await behavior.Invoke(context, () => { throw new Exception("exception-message"); });

            if (transactionMode != TransportTransactionMode.None)
            {
                await behavior.Invoke(context, () => Task.FromResult(0));
            }

            var notification = eventAggregator.GetNotification<MessageFaulted>();

            Assert.AreEqual("some-id", notification.Message.MessageId);
            Assert.AreEqual("exception-message", notification.Exception.Message);
        }

        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task ShouldInvokePipelineOnlyOnceWhenErrorIsThrown(TransportTransactionMode transactionMode)
        {
            var behavior = CreateBehavior(transactionMode);
            var context = CreateContext();
            var invokedTwice = false;

            await behavior.Invoke(context, () => { throw new Exception("exception-message"); });
            await behavior.Invoke(context, () =>
            {
                invokedTwice = true;
                return Task.FromResult(0);
            });

            Assert.IsFalse(invokedTwice, "Pipline continuation should not be called when failed message is processed second time.");
        }

        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task ShouldInvokePipelineWhenFaultedMessageRedeliveredImmediately(TransportTransactionMode transactionMode)
        {
            var fakeDispatcher = new FakeDispatcher();

            var behavior = CreateBehavior(transactionMode, dispatcher: fakeDispatcher);
            var context = CreateContext();

            await behavior.Invoke(context, () => { throw new Exception(); });

            Assert.Null(fakeDispatcher.ErrorOperation);

            await behavior.Invoke(context, () =>
            {
                Assert.Fail("Should not call the main pipeline");

                return TaskEx.CompletedTask;
            });

            Assert.NotNull(fakeDispatcher.ErrorOperation);
        }

        [Test]
        public async Task ShouldEnrichHeadersWithExceptionDetails()
        {
            var messageId = "message-id";
            var fakeDispatcher = new FakeDispatcher();

            var context = CreateContext(messageId);
            var behavior = CreateBehavior(TransportTransactionMode.None, new Dictionary<string, string> { { "MyKey", "MyValue" } }, fakeDispatcher);

            await behavior.Invoke(context, c =>
            {
                throw new Exception("exception-message");
            });

            var messageSentToError = fakeDispatcher.ErrorOperation.Message;

            Assert.AreEqual("MyValue", messageSentToError.Headers["MyKey"]);
            Assert.AreEqual("exception-message", messageSentToError.Headers["NServiceBus.ExceptionInfo.Message"]);
        }

        MoveFaultsToErrorQueueBehavior CreateBehavior(TransportTransactionMode transactionMode, Dictionary<string, string> staticFaultMetadata = null, IDispatchMessages dispatcher = null)
        {
            if (dispatcher == null)
            {
                dispatcher = new FakeDispatcher();
            }

            var behavior = new MoveFaultsToErrorQueueBehavior(
                criticalError,
                staticFaultMetadata ?? new Dictionary<string, string>(),
                transactionMode,
                new FailureInfoStorage(10),
                dispatcher,
                "error");

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