namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using Faults;
    using Hosting;
    using NServiceBus.Core.Tests.Recoverability.SecondLevelRetries;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using NServiceBus.Routing;
    using TransportDispatch;
    using Transports;
    using Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardFaultsToErrorQueueTests
    {
        FakeDispatchPipeline pipeline;
        FakeCriticalError errors;
        HostInformation hostInfo;
        BusNotifications notifications;

        [SetUp]
        public void Setup()
        {
            pipeline = new FakeDispatchPipeline();
            errors = new FakeCriticalError();
            hostInfo = new HostInformation(Guid.NewGuid(), "my host");
            notifications = new BusNotifications();
        }

        RecoverabilityBehavior CreateBehavior(string errorQueueAddress)
        {
            var flrHandler = new FirstLevelRetriesHandler(new FlrStatusStorage(), new FirstLevelRetryPolicy(0), notifications);
            var slrHandler = new SecondLevelRetriesHandler(pipeline, new FakePolicy(TimeSpan.MinValue), notifications, string.Empty, false);

            var bahavior = new RecoverabilityBehavior(
                errors,
                pipeline,
                hostInfo,
                notifications, 
                errorQueueAddress, 
                flrHandler,
                slrHandler);

            return bahavior;
        }

        [Test]
        public async Task ShouldForwardToErrorQueueForAllExceptions()
        {
            var errorQueueAddress = "error";
            var behavior = CreateBehavior(errorQueueAddress); 

            var context = CreateContext("someid");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));

            await SimulateFailingExecution(behavior, context);

            Assert.AreEqual(errorQueueAddress, pipeline.Destination);

            Assert.AreEqual("someid", pipeline.MessageSent.MessageId);
        }

        [Test]
        public void ShouldInvokeCriticalErrorIfForwardingFails()
        {
            pipeline.ThrowOnDispatch = true;

            var behavior = CreateBehavior("error");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));

            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.Throws<Exception>(async () => await SimulateFailingExecution(behavior, CreateContext("someid")));

            Assert.True(errors.ErrorRaised);
        }


        [Test]
        public async Task ShouldEnrichHeadersWithHostAndExceptionDetails()
        {
            var context = CreateContext("someid");


            var behavior = CreateBehavior("error");
            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));

            await SimulateFailingExecution(behavior, context);

            //host info
            Assert.AreEqual(hostInfo.HostId.ToString("N"), pipeline.MessageSent.Headers[Headers.HostId]);
            Assert.AreEqual(hostInfo.DisplayName, pipeline.MessageSent.Headers[Headers.HostDisplayName]);

            Assert.AreEqual("public-receive-address", pipeline.MessageSent.Headers[FaultsHeaderKeys.FailedQ]);
            //exception details
            Assert.AreEqual("testex", pipeline.MessageSent.Headers["NServiceBus.ExceptionInfo.Message"]);

        }

        [Test]
        public async Task ShouldRaiseNotificationWhenMessageIsForwarded()
        {


            var behavior = CreateBehavior("error");
            var failedMessageNotification = new FailedMessage();

            notifications.Errors.MessageSentToErrorQueue.Subscribe(f => { failedMessageNotification = f; });

            behavior.Initialize(new PipelineInfo("Test", "public-receive-address"));
            await SimulateFailingExecution(behavior, CreateContext("someid"));

            Assert.AreEqual("someid", failedMessageNotification.MessageId);

            Assert.AreEqual("testex", failedMessageNotification.Exception.Message);
        }

        static async Task SimulateFailingExecution(RecoverabilityBehavior behavior, TransportReceiveContext context)
        {
            try
            {
                await behavior.Invoke(context, () => { throw new Exception("testex"); });
            }
            catch (MessageProcessingAbortedException)
            {
            }

            //We need to call behavior twice since moving to error queue is performed on next message receive
            await behavior.Invoke(context, () => Task.FromResult(0));
        }

        TransportReceiveContext CreateContext(string messageId)
        {
            return new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), new RootContext(null));
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