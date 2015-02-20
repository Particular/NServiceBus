namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Core.Tests.Features;
    using NServiceBus.Faults;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardFaultsToErrorQueueTests
    {


        [Test]
        public void ShouldForwardToErrorQueueForAllExceptions()
        {
            var sender = new FakeSender();
            var errorQueueAddress = "error";
            var behavior = new MoveFaultsToErrorQueueBehavior(new FakeCriticalError(), 
                sender, 
                new HostInformation(Guid.NewGuid(), "my host"),
                new BusNotifications(), errorQueueAddress);

            var context = CreateContext("someid");

            behavior.Invoke(context, () =>
            {
                throw new Exception("testex");
            });

            Assert.AreEqual(Address.Parse(errorQueueAddress), sender.OptionsUsed.Destination);

            Assert.AreEqual("someid", sender.MessageSent.Id);
        }
        [Test]
        public void ShouldInvokeCriticalErrorIfForwardingFails()
        {
            var criticalError = new FakeCriticalError();

            var behavior = new MoveFaultsToErrorQueueBehavior(criticalError, new FakeSender
            {
                ThrowOnSend = true
            }, new HostInformation(Guid.NewGuid(), "my host"), new BusNotifications(), "error");


            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.Throws<Exception>(() => behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("testex");
            }));

            Assert.True(criticalError.ErrorRaised);
        }


        [Test]
        public void ShouldEnrichHeadersWithHostAndExceptionDetails()
        {
            var sender = new FakeSender();
            var hostInfo = new HostInformation(Guid.NewGuid(), "my host");
            var context = CreateContext("someid");


            var behavior = new MoveFaultsToErrorQueueBehavior(new FakeCriticalError(), sender, hostInfo, new BusNotifications(), "error");

            behavior.Invoke(context, () =>
            {
                throw new Exception("testex");
            });

            //host info
            Assert.AreEqual(hostInfo.HostId.ToString("N"), sender.MessageSent.Headers[Headers.HostId]);
            Assert.AreEqual(hostInfo.DisplayName, sender.MessageSent.Headers[Headers.HostDisplayName]);

            Assert.AreEqual(context.PublicReceiveAddress(), sender.MessageSent.Headers[FaultsHeaderKeys.FailedQ]);
            //exception details
            Assert.AreEqual("testex", sender.MessageSent.Headers["NServiceBus.ExceptionInfo.Message"]);

        }

        [Test]
        public void ShouldRaiseNotificationWhenMessageIsForwarded()
        {

            var notifications = new BusNotifications();
            var sender = new FakeSender();
            var behavior = new MoveFaultsToErrorQueueBehavior(new FakeCriticalError(), 
                sender, 
                new HostInformation(Guid.NewGuid(), "my host"),
                notifications, 
                "error");
            var failedMessageNotification = new FailedMessage();

            notifications.Errors.MessageSentToErrorQueue.Subscribe(f => { failedMessageNotification = f; });

            behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("testex");
            });



            Assert.AreEqual("someid", failedMessageNotification.Headers[Headers.MessageId]);

            Assert.AreEqual("testex", failedMessageNotification.Exception.Message);
        }



        PhysicalMessageProcessingStageBehavior.Context CreateContext(string messageId)
        {
            var transportMessage = new TransportMessage(messageId, new Dictionary<string, string>());

            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(transportMessage, null));

            context.SetPublicReceiveAddress("public-receive-address");
            return context;
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
        public class FakeSender : ISendMessages
        {
            public TransportMessage MessageSent { get; set; }

            public SendOptions OptionsUsed { get; set; }
            public bool ThrowOnSend { get; set; }

            public void Send(TransportMessage message, SendOptions sendOptions)
            {
                MessageSent = message;
                OptionsUsed = sendOptions;

                if (ThrowOnSend)
                {
                    throw new Exception("Failed to send");
                }
            }
        }
    }
}