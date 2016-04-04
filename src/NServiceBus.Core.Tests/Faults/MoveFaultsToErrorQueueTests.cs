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
        [Test]
        public async Task ShouldForwardToErrorQueueForAllExceptions()
        {
            var errorQueueAddress = "error";
            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                errorQueueAddress,
                "public-receive-address");

            var context = CreateContext("someid");

            IFaultContext faultContext = null;

            await behavior.Invoke(context, () => { throw new Exception("testex"); }, c => CaptureFaultContext(c, out faultContext));

            Assert.IsNotNull(faultContext, "it should forward message to error queue");
            Assert.AreEqual(errorQueueAddress, faultContext.ErrorQueueAddress);
            Assert.AreEqual("someid", faultContext.Message.MessageId);
        }

        [Test]
        public void ShouldInvokeCriticalErrorIfForwardingFails()
        {
            var criticalError = new FakeCriticalError();

            var behavior = new MoveFaultsToErrorQueueBehavior(
                criticalError,
                "error",
                "public-receive-address");

            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.That(async () => await behavior.Invoke(CreateContext("someid"), () => { throw new Exception("testex"); }, context => { throw new Exception("Failed to dispatch"); }), Throws.InstanceOf<Exception>());
            Assert.True(criticalError.ErrorRaised);
        }

        [Test]
        public async Task ShouldEnrichHeadersWithExceptionDetails()
        {
            var context = CreateContext("someid");

            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                "error",
                "public-receive-address");

            IFaultContext faultContext = null;
            await behavior.Invoke(context, () => { throw new Exception("testex"); }, c => CaptureFaultContext(c, out faultContext));

            Assert.IsNotNull(faultContext, "it should forward message to error queue");
            Assert.AreEqual("public-receive-address", faultContext.Message.Headers[FaultsHeaderKeys.FailedQ]);
            //exception details
            Assert.AreEqual("testex", faultContext.Message.Headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public async Task ShouldRegisterFailureInfoWhenMessageIsForwarded()
        {
            var eventAggregator = new FakeEventAggregator();

            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                "error",
                "public-receive-address");

            var context = CreateContext("someid", eventAggregator);

            await behavior.Invoke(context, () =>
            {
                throw new Exception("testex");
            }, fc => TaskEx.CompletedTask);

            var notification = eventAggregator.GetNotification<MessageFaulted>();

            Assert.AreEqual("someid", notification.Message.MessageId);
            Assert.AreEqual("testex", notification.Exception.Message);
        }

        static Task CaptureFaultContext(IFaultContext ctx, out IFaultContext context)
        {
            context = ctx;
            return TaskEx.CompletedTask;
        }

        static TestableTransportReceiveContext CreateContext(string messageId, FakeEventAggregator eventAggregator = null)
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