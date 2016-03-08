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

    [TestFixture]
    public class MoveFaultsToErrorQueueTests
    {
        [Test]
        public async Task ShouldForwardToErrorQueueForAllExceptions()
        {
            var fakeFaultPipeline = new FakeFaultPipeline();
            var errorQueueAddress = "error";
            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
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
                "error",
                "public-receive-address");

            await behavior.Invoke(context, () => { throw new Exception("testex"); });

            Assert.AreEqual("public-receive-address", fakeFaultPipeline.MessageSent.Headers[FaultsHeaderKeys.FailedQ]);
            //exception details
            Assert.AreEqual("testex", fakeFaultPipeline.MessageSent.Headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public async Task ShouldRegisterFailureInfoWhenMessageIsForwarded()
        {
            var fakeFaultPipeline = new FakeFaultPipeline();

            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                "error",
                "public-receive-address");


            var context = CreateContext("someid", fakeFaultPipeline);


            await behavior.Invoke(context, () =>
            {
                throw new Exception("testex");
            });

            var notification = context.GetNotification<MessageFaulted>();

            Assert.AreEqual("someid", notification.Message.MessageId);
            Assert.AreEqual("testex", notification.Exception.Message);
        }

        static FakeTransportReceiveContext CreateContext(string messageId, FakeFaultPipeline pipeline)
        {
            var context = new FakeTransportReceiveContext(messageId);

            context.Extensions.Set<IPipelineCache>(new FakePipelineCache(pipeline));

            return context;
        }

        class FakeTransportReceiveContext : FakeBehaviorContext, ITransportReceiveContext
        {
            public FakeTransportReceiveContext(string messageId)
            {

                Message = new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream());
            }

            public bool ReceiveOperationWasAborted { get; private set; }
            
            public IncomingMessage Message { get; }

            public void AbortReceiveOperation()
            {
                ReceiveOperationWasAborted = true;
            }
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