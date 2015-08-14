namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Core.Tests.Features;
    using NServiceBus.Faults;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardFaultsToErrorQueueTests
    {


        [Test]
        public void ShouldForwardToErrorQueueForAllExceptions()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var errorQueueAddress = "error";
            var behavior = new MoveFaultsToErrorQueueBehavior(new FakeCriticalError(),
                fakeDispatchPipeline, 
                new HostInformation(Guid.NewGuid(), "my host"),
                new BusNotifications(), errorQueueAddress);

            var context = CreateContext("someid");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));

            behavior.Invoke(context, () =>
            {
                throw new Exception("testex");
            });

            Assert.AreEqual(errorQueueAddress, fakeDispatchPipeline.Destination);

            Assert.AreEqual("someid", fakeDispatchPipeline.MessageSent.Headers[Headers.MessageId]);
        }
        [Test]
        public void ShouldInvokeCriticalErrorIfForwardingFails()
        {
            var criticalError = new FakeCriticalError();
            var fakeDispatchPipeline = new FakeDispatchPipeline{ThrowOnDispatch = true};


            var behavior = new MoveFaultsToErrorQueueBehavior(criticalError, fakeDispatchPipeline, new HostInformation(Guid.NewGuid(), "my host"), new BusNotifications(), "error");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));

            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.Throws<Exception>(async () => await behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("testex");
            }));

            Assert.True(criticalError.ErrorRaised);
        }


        [Test]
        public void ShouldEnrichHeadersWithHostAndExceptionDetails()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var hostInfo = new HostInformation(Guid.NewGuid(), "my host");
            var context = CreateContext("someid");


            var behavior = new MoveFaultsToErrorQueueBehavior(new FakeCriticalError(), fakeDispatchPipeline, hostInfo, new BusNotifications(), "error");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));
            behavior.Invoke(context, () =>
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
        public void ShouldRaiseNotificationWhenMessageIsForwarded()
        {

            var notifications = new BusNotifications();
            var fakeDispatchPipeline = new FakeDispatchPipeline();
         
            var behavior = new MoveFaultsToErrorQueueBehavior(new FakeCriticalError(),
                fakeDispatchPipeline, 
                new HostInformation(Guid.NewGuid(), "my host"),
                notifications, 
                "error");
            var failedMessageNotification = new FailedMessage();

            notifications.Errors.MessageSentToErrorQueue.Subscribe(f => { failedMessageNotification = f; });

            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));
            behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("testex");
            });



            Assert.AreEqual("someid", failedMessageNotification.Headers[Headers.MessageId]);

            Assert.AreEqual("testex", failedMessageNotification.Exception.Message);
        }



        PhysicalMessageProcessingStageBehavior.Context CreateContext(string messageId)
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), null));
            return context;
        }
        class FakeDispatchPipeline : IPipelineBase<DispatchContext>
        {
            public string Destination { get; private set; }
            public OutgoingMessage MessageSent { get; private set; }
            public bool ThrowOnDispatch { get; set; }

            public void Invoke(DispatchContext context)
            {
                if (ThrowOnDispatch)
                {
                    throw new Exception("Failed to dispatch");
                }

                Destination = ((DirectToTargetDestination) context.GetRoutingStrategy()).Destination;
                MessageSent = context.Get<OutgoingMessage>();
            }
        }
        class FakeCriticalError : CriticalError
        {
            public FakeCriticalError()
                : base((s, e) => { }, new FakeBuilder())
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