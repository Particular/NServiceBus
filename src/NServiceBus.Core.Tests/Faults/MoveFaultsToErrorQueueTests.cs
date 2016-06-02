namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Testing;
    using Timeout;
    using Timeout.TimeoutManager;

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
        public async Task ShouldForwardToErrorQueue(TransportTransactionMode transactionMode)
        {
            var behavior = CreateBehavior();
            var context = CreateContext("some-id");
            var dispatcher = new RecordingFakeDispatcher();

            await behavior.Invoke("error-queue", context.Message, new Exception(), dispatcher, new ContextBag());

            Assert.AreEqual(1, dispatcher.DispatchedMessages);
            Assert.AreEqual(
                context.Message.MessageId, 
                dispatcher.DispatchedMessages[0].Operations.UnicastTransportOperations.First().Message.MessageId);
        }

        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task ShouldInvokeCriticalErrorIfForwardingFails(TransportTransactionMode transactionMode)
        {
            var behavior = CreateBehavior();
            var context = CreateContext();

            await behavior.Invoke(string.Empty, context.Message, new Exception(), new FailingMessageDispatcher(), new ContextBag()).ConfigureAwait(false);

            Assert.True(criticalError.ErrorRaised);
        }

        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task ShouldRegisterFailureInfoWhenMessageIsForwarded(TransportTransactionMode transactionMode)
        {
            var behavior = CreateBehavior();
            var eventAggregator = new FakeEventAggregator();
            var context = CreateContext("some-id", eventAggregator);

            await behavior.Invoke(string.Empty, context.Message, new Exception("exception-message"), new FakeMessageDispatcher(), new ContextBag()).ConfigureAwait(false);

            var notification = eventAggregator.GetNotification<MessageFaulted>();

            Assert.AreEqual("some-id", notification.Message.MessageId);
            Assert.AreEqual("exception-message", notification.Exception.Message);
        }

        class FailingMessageDispatcher : IDispatchMessages
        {
            public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
            {
                throw new Exception();
            }
        }

        MoveFaultsToErrorQueueBehavior CreateBehavior()
        {
            var behavior = new MoveFaultsToErrorQueueBehavior(criticalError);

            return behavior;
        }

        static Task CaptureFaultContext(IFaultContext ctx, out IFaultContext context)
        {
            context = ctx;
            return TaskEx.CompletedTask;
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