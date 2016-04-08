namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Faults;
    using NServiceBus.Pipeline;
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
            var behavior = CreateBehavior(transactionMode, "errors-queue");
            var context = CreateContext("some-id");

            IFaultContext faultContext = null;

            await behavior.Invoke(context, () => { throw new Exception(); }, c => CaptureFaultContext(c, out faultContext));

            if (transactionMode != TransportTransactionMode.None)
            {
                await behavior.Invoke(context, () => Task.FromResult(0), c => CaptureFaultContext(c, out faultContext));
            }

            Assert.AreEqual("errors-queue", faultContext.ErrorQueueAddress);
            Assert.AreEqual("some-id", faultContext.Message.MessageId);
        }

        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public void ShouldInvokeCriticalErrorIfForwardingFails(TransportTransactionMode transactionMode)
        {
            var behavior = CreateBehavior(transactionMode);
            var context = CreateContext();

            var behaviorInvocation = new TestDelegate(async () =>
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
        public async Task ShouldEnrichHeadersWithExceptionDetails(TransportTransactionMode transactionMode)
        {
            var context = CreateContext();
            var behavior = CreateBehavior(transactionMode);

            IFaultContext faultContext = null;

            await behavior.Invoke(context, () => { throw new Exception("exception-message"); }, c => CaptureFaultContext(c, out faultContext));

            if (transactionMode != TransportTransactionMode.None)
            {
                await behavior.Invoke(context, () => Task.FromResult(0), c => CaptureFaultContext(c, out faultContext));
            }

            Assert.AreEqual("public-receive-address", faultContext.Message.Headers[FaultsHeaderKeys.FailedQ]);
            Assert.AreEqual("exception-message", faultContext.Message.Headers["NServiceBus.ExceptionInfo.Message"]);
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

            await behavior.Invoke(context, () => { throw new Exception("exception-message"); }, c => Task.FromResult(0));

            if (transactionMode != TransportTransactionMode.None)
            {
                await behavior.Invoke(context, () => Task.FromResult(0), c => Task.FromResult(0));
            }

            var notification = eventAggregator.GetNotification<MessageFaulted>();

            Assert.AreEqual("some-id", notification.Message.MessageId);
            Assert.AreEqual("exception-message", notification.Exception.Message);
        }

        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task ShoulInvokePipelineOnlyOnceWhenErrorIsThrown(TransportTransactionMode transactionMode)
        {
            var behavior = CreateBehavior(transactionMode);
            var context = CreateContext();
            var invokedTwice = false;

            await behavior.Invoke(context, () => { throw new Exception("exception-message"); }, c => Task.FromResult(0));
            await behavior.Invoke(context, () =>
            {
                invokedTwice = true;
                return Task.FromResult(0);
            }, c => Task.FromResult(0));

            Assert.IsFalse(invokedTwice, "Pipline continuation should not be called when failed message is processed second time.");
        }

        MoveFaultsToErrorQueueBehavior CreateBehavior(TransportTransactionMode transactionMode, string errorQueueAddress = "errors")
        {
            var behavior = new MoveFaultsToErrorQueueBehavior(
                criticalError,
                errorQueueAddress,
                "public-receive-address",
                transactionMode,
                new FailureInfoStorage(10));

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