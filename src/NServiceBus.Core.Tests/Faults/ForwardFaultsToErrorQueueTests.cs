namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Faults;
    using Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using TransportDispatch;
    using Transports;
    using Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardFaultsToErrorQueueTests
    {
        [Test]
        public async Task ShouldForwardToErrorQueueForAllExceptions()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var errorQueueAddress = "error";
            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                fakeDispatchPipeline, 
                new HostInformation(Guid.NewGuid(), "my host"),
                new List<Action<FailedMessage>>(), 
                errorQueueAddress);

            var context = CreateContext("someid");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));

            await behavior.Invoke(context, () =>
            {
                throw new Exception("testex");
            });

            Assert.AreEqual(errorQueueAddress, fakeDispatchPipeline.Destination);

            Assert.AreEqual("someid", fakeDispatchPipeline.MessageSent.MessageId);
        }

        [Test]
        public void ShouldInvokeCriticalErrorIfForwardingFails()
        {
            var criticalError = new FakeCriticalError();
            var fakeDispatchPipeline = new FakeDispatchPipeline{ThrowOnDispatch = true};


            var behavior = new MoveFaultsToErrorQueueBehavior(
                criticalError, 
                fakeDispatchPipeline, 
                new HostInformation(Guid.NewGuid(), "my host"),
                new List<Action<FailedMessage>>(), 
                "error");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));

            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.Throws<Exception>(async () => await behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("testex");
            }));

            Assert.True(criticalError.ErrorRaised);
        }

        [Test]
        public async Task ShouldEnrichHeadersWithHostAndExceptionDetails()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var hostInfo = new HostInformation(Guid.NewGuid(), "my host");
            var context = CreateContext("someid");


            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(), 
                fakeDispatchPipeline, 
                hostInfo,
                new List<Action<FailedMessage>>(), 
                "error");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));

            await behavior.Invoke(context, () =>
            {
                throw new Exception("testex");
            });

            //host info
            Assert.AreEqual(hostInfo.HostId.ToString("N"), fakeDispatchPipeline.MessageSent.Headers[Headers.HostId]);
            Assert.AreEqual(hostInfo.DisplayName, fakeDispatchPipeline.MessageSent.Headers[Headers.HostDisplayName]);

            Assert.AreEqual("public-receive-address", fakeDispatchPipeline.MessageSent.Headers[FaultsHeaderKeys.FailedQ]);
            //exception details
            Assert.AreEqual("testex", fakeDispatchPipeline.MessageSent.Headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public async Task ShouldRaiseNotificationWhenMessageIsForwarded()
        {
            var failedMessageNotification = new FailedMessage();
            var fakeDispatchPipeline = new FakeDispatchPipeline();
         
            var behavior = new MoveFaultsToErrorQueueBehavior(
                new FakeCriticalError(),
                fakeDispatchPipeline, 
                new HostInformation(Guid.NewGuid(), "my host"),new List<Action<FailedMessage>>
                {
                    failedMessage => failedMessageNotification = failedMessage
                }, 
                "error");

            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));
            await behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("testex");
            });

            Assert.AreEqual("someid", failedMessageNotification.MessageId);

            Assert.AreEqual("testex", failedMessageNotification.Exception.Message);
        }
        
        TransportReceiveContext CreateContext(string messageId)
        {
            return new TransportReceiveContextImpl(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), null, new RootContext(null));
        }
        class FakeDispatchPipeline : IPipelineBase<RoutingContext>
        {
            public string Destination { get; private set; }
            public OutgoingMessage MessageSent { get; private set; }
            public bool ThrowOnDispatch { get; set; }

            public Task Invoke(RoutingContext context)
            {
                if (ThrowOnDispatch)
                {
                    throw new Exception("Failed to dispatch");
                }

                Destination = ((UnicastAddressTag) context.RoutingStrategies.First().Apply(new Dictionary<string, string>())).Destination;
                MessageSent = context.Message;
                return Task.FromResult(0);
            }
        }

        class FakeCriticalError : CriticalError
        {
            public FakeCriticalError() : base((d, s, e) => TaskEx.Completed)
            {
            }

            public override void Raise(string errorMessage, Exception exception)
            {
                ErrorRaised = true;
            }

            public bool ErrorRaised { get; private set; }
        }
    }
}