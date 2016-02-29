namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardFaultsToErrorQueueTests
    {
        [Test]
        public async Task ShouldForwardToErrorQueueForAllExceptions()
        {
            var fakeFaultPipeline = new FakeFaultPipeline();
            var errorQueueAddress = "error";
            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                new BusNotifications(),
                errorQueueAddress,
                "public-receive-address");

            var context = CreateContext("someid", fakeFaultPipeline);

            await behavior.Invoke(context, () => { throw new Exception("testex"); });

            Assert.AreEqual(errorQueueAddress, fakeFaultPipeline.Destination);

            Assert.AreEqual("someid", fakeFaultPipeline.MessageSent.MessageId);
        }

        [Test]
        public void ShouldInvokeCriticalErrorIfForwardingFails()
        {
            var criticalError = new FakeCriticalError();
            var fakeDispatchPipeline = new FakeFaultPipeline { ThrowOnDispatch = true };

            var behavior = new MoveFaultsToErrorQueueBehavior(
                criticalError,
                new BusNotifications(),
                "error",
                "public-receive-address");

            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.That(async () => await behavior.Invoke(CreateContext("someid", fakeDispatchPipeline), () => { throw new Exception("testex"); }), Throws.InstanceOf<Exception>());
            Assert.True(criticalError.ErrorRaised);
        }

        [Test]
        public async Task ShouldEnrichHeadersWithExceptionDetails()
        {
            var fakeFaultPipeline = new FakeFaultPipeline();
            var context = CreateContext("someid", fakeFaultPipeline);

            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                new BusNotifications(),
                "error",
                "public-receive-address");

            await behavior.Invoke(context, () => { throw new Exception("testex"); });

            Assert.AreEqual("public-receive-address", fakeFaultPipeline.MessageSent.Headers[FaultsHeaderKeys.FailedQ]);
            //exception details
            Assert.AreEqual("testex", fakeFaultPipeline.MessageSent.Headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public async Task ShouldRaiseNotificationWhenMessageIsForwarded()
        {
            var notifications = new BusNotifications();
            var fakeFaultPipeline = new FakeFaultPipeline();

            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                notifications,
                "error",
                "public-receive-address");
            var failedMessageNotification = new FailedMessage();

            notifications.Errors.MessageSentToErrorQueue += (sender, message) => failedMessageNotification = message;

            await behavior.Invoke(CreateContext("someid", fakeFaultPipeline), () =>
            {
                throw new Exception("testex");
            });

            Assert.AreEqual("someid", failedMessageNotification.MessageId);

            Assert.AreEqual("testex", failedMessageNotification.Exception.Message);
        }

        static ITransportReceiveContext CreateContext(string messageId, FakeFaultPipeline pipeline)
        {
            return new TransportReceiveContext(messageId, new Dictionary<string, string>(), new MemoryStream(), null, new CancellationTokenSource(), new RootContext(null, new FakePipelineCache(pipeline)));
        }

        class FakePipelineCache : IPipelineCache
        {
            IPipeline<IFaultContext> pipeline;

            public FakePipelineCache(IPipeline<IFaultContext> pipeline)
            {
                this.pipeline = pipeline;
            }

            public IPipeline<TContext> Pipeline<TContext>()
                where TContext : IBehaviorContext

            {
                return (IPipeline<TContext>)pipeline;
            }
        }

        class FakeFaultPipeline : IPipeline<IFaultContext>
        {
            public string Destination { get; private set; }
            public OutgoingMessage MessageSent { get; private set; }
            public bool ThrowOnDispatch { get; set; }

            public Task Invoke(IFaultContext context)
            {
                if (ThrowOnDispatch)
                {
                    throw new Exception("Failed to dispatch");
                }

                Destination = context.ErrorQueueAddress;
                MessageSent = context.Message;
                return TaskEx.CompletedTask;
            }
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