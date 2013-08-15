namespace NServiceBus.Testing.Tests
{
    using System;
    using NUnit.Framework;


    [TestFixture]
    public class TestHandlerFixture
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Test.Initialize();
        }

        [Test]
        public void ShouldAssertDoNotContinueDispatchingCurrentMessageToHandlersWasCalled()
        {
            Test.Handler<DoNotContinueDispatchingCurrentMessageToHandlersHandler>()
                .ExpectDoNotContinueDispatchingCurrentMessageToHandlers()
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailAssertingDoNotContinueDispatchingCurrentMessageToHandlersWasCalled()
        {
            Test.Handler<EmptyHandler>()
                .ExpectDoNotContinueDispatchingCurrentMessageToHandlers()
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldAssertHandleCurrentMessageLaterWasCalled()
        {
            Test.Handler<HandleCurrentMessageLaterHandler>()
                .ExpectHandleCurrentMessageLater()
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailAssertingHandleCurrentMessageLaterWasCalled()
        {
            Test.Handler<EmptyHandler>()
                .ExpectHandleCurrentMessageLater()
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldCallHandleOnExplicitInterfaceImplementation()
        {
            var handler = new ExplicitInterfaceImplementation();
            Assert.IsFalse(handler.IsHandled);
            Test.Handler(handler).OnMessage<TestMessage>();
            Assert.IsTrue(handler.IsHandled);
        }

        [Test]
        public void ShouldPassExpectPublishWhenPublishing()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .ExpectPublish<Publish1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectNotPublishWhenPublishing()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .ExpectNotPublish<Publish1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectPublishWhenPublishingMultipleEvents()
        {
            Test.Handler<PublishingHandler<Publish1, Publish2>>()
                .ExpectPublish<Publish1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectPublishWhenMessageIsSend()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .ExpectPublish<Publish1>(m => true)
                .OnMessage(new TestMessageImpl(), Guid.NewGuid().ToString());
        }


        [Test]
        public void ShouldPassExpectPublishWhenPublishingAndCheckingPredicate()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .WithExternalDependencies(h => h.ModifyPublish = m => m.Data = "Data")
                .ExpectPublish<Publish1>(m => m.Data == "Data")
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectNotPublishWhenPublishingAndCheckingPredicate()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .WithExternalDependencies(h => h.ModifyPublish = m => m.Data = "Data")
                .ExpectNotPublish<Publish1>(m => m.Data == "Data")
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectPublishWhenPublishingAndCheckingPredicateThatFails()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .WithExternalDependencies(h => h.ModifyPublish = m => m.Data = "NotData")
                .ExpectPublish<Publish1>(m => m.Data == "Data")
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectNotPublishWhenPublishingAndCheckingPredicateThatFails()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .WithExternalDependencies(h => h.ModifyPublish = m => m.Data = "NotData")
                .ExpectNotPublish<Publish1>(m => m.Data == "Data")
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectPublishIfNotPublishing()
        {
            Test.Handler<EmptyHandler>()
                .ExpectPublish<Publish1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectNotPublishIfNotPublishing()
        {
            Test.Handler<EmptyHandler>()
                .ExpectNotPublish<Publish1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectSendIfSending()
        {
            Test.Handler<SendingHandler<Send1>>()
                .ExpectSend<Send1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectSendIfNotSending()
        {
            Test.Handler<EmptyHandler>()
                .ExpectSend<Send1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectSendIfSendingWithoutMatch()
        {
            Test.Handler<SendingHandler<Publish1>>()
                .ExpectSend<Send1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectNotSendIfNotSending()
        {
            Test.Handler<EmptyHandler>()
                .ExpectNotSend<Send1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectNotSendIfSending()
        {
            Test.Handler<SendingHandler<Send1>>()
                .ExpectNotSend<Send1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectNotSendLocalIfNotSendingLocal()
        {
            Test.Handler<EmptyHandler>()
                .ExpectNotSendLocal<Send1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectNotSendLocalIfSendingLocal()
        {
            Test.Handler<SendingLocalHandler<Send1>>()
                .ExpectNotSendLocal<Send1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectNotSendLocalIfSendingLocalWithoutMatch()
        {
            Test.Handler<SendingLocalHandler<Publish1>>()
                .ExpectNotSendLocal<Send1>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        [ExpectedException]
        public void ShouldFailExpectPublishIfPublishWrongMessageType()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .ExpectPublish<Publish2>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldPassExpectNotPublishIfPublishWrongMessageType()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .ExpectNotPublish<Publish2>(m => true)
                .OnMessage<TestMessage>();
        }

        [Test]
        public void ShouldSupportDataBusProperties()
        {
            Test.Handler<DataBusMessageHandler>()
                .ExpectNotPublish<Publish2>(m => true)
                .OnMessage<MessageWithDataBusProperty>();
        }

        [Test]
        public void ShouldSupportSendingManyMessagesAtOnce()
        {
            Test.Handler<SendingManyHandler>()
                .ExpectSend<Outgoing>(m => m.Number == 1)
                .OnMessage<Incoming>();

            Test.Handler<SendingManyHandler>()
                .ExpectSend<Outgoing>(m => m.Number == 2)
                .OnMessage<Incoming>();
        }

        [Test]
        public void ShouldSupportSendingDifferentMessagesAtOnce()
        {
            var result = 0;

            Test.Handler<SendingManyWithDifferentMessagesHandler>()
                .ExpectSend<Outgoing>(m =>
                {
                    result += m.Number;
                    return true;
                })
                .ExpectSend<Outgoing2>(m =>
                {
                    result += m.Number;
                    return true;
                })
                .OnMessage<Incoming>();

            Assert.AreEqual(3, result);
        }

        [Test]
        public void ShouldSupportPublishMoreThanOneMessageAtOnce()
        {
            Test.Handler<PublishingManyHandler>()
                .ExpectPublish<Outgoing>(m => true)
                .ExpectPublish<Outgoing>(m => true)
                .OnMessage<Incoming>();
        }

        [Test]
        public void Should_be_able_to_pass_an_already_constructed_message_into_handler_without_specifying_id()
        {
            const string expected = "dummy";

            var handler = new TestMessageWithPropertiesHandler();
            var message = new TestMessageWithProperties { Dummy = expected };
            Test.Handler(handler)
              .OnMessage(message);
            Assert.AreEqual(expected, handler.ReceivedDummyValue);
            Assert.DoesNotThrow(() => Guid.Parse(handler.AssignedMessageId), "Message ID should be a valid GUID.");
        }


        private class TestMessageImpl : TestMessage
        {
        }

        public class EmptyHandler : IHandleMessages<TestMessage>
        {
            public void Handle(TestMessage message)
            {
            }
        }

        public interface Publish1 : IMessage
        {
            string Data { get; set; }
        }

        public interface Send1 : IMessage
        {
            string Data { get; set; }
        }

        public interface Publish2 : IMessage
        {
            string Data { get; set; }
        }

        public class Outgoing : IMessage
        {
            public int Number { get; set; }
        }

        public class Outgoing2 : IMessage
        {
            public int Number { get; set; }
        }

        public class Incoming : IMessage
        {
        }

        public class PublishingManyHandler : IHandleMessages<Incoming>
        {
            public void Handle(Incoming message)
            {
                var one = this.Bus().CreateInstance<Outgoing>(m =>
                {
                    m.Number = 1;
                });

                var two = this.Bus().CreateInstance<Outgoing>(m =>
                {
                    m.Number = 2;
                });

                ((StubBus)this.Bus()).Publish(one, two);
            }
        }

        public class SendingManyWithDifferentMessagesHandler : IHandleMessages<Incoming>
        {
            public IBus Bus { get; set; }

            public void Handle(Incoming message)
            {
                var one = Bus.CreateInstance<Outgoing>(m =>
                {
                    m.Number = 1;
                });

                var two = Bus.CreateInstance<Outgoing2>(m =>
                {
                    m.Number = 2;
                });

                ((StubBus)Bus).Send(one, two);
            }
        }

        public class SendingManyHandler : IHandleMessages<Incoming>
        {
            public IBus Bus { get; set; }

            public void Handle(Incoming message)
            {
                var one = Bus.CreateInstance<Outgoing>(m =>
                {
                    m.Number = 1;
                });

                var two = Bus.CreateInstance<Outgoing>(m =>
                {
                    m.Number = 2;
                });

                ((StubBus)Bus).Send(one, two);
            }
        }

        public class SendingHandler<TSend> : IHandleMessages<TestMessage>
            where TSend : IMessage
        {
            public Action<TSend> ModifyPublish { get; set; }

            public void Handle(TestMessage message)
            {
                this.Bus().Send(ModifyPublish);
            }
        }

        public class SendingLocalHandler<TSend> : IHandleMessages<TestMessage>
            where TSend : IMessage
        {
            public IBus Bus { get; set; }
            public Action<TSend> ModifyPublish { get; set; }

            public void Handle(TestMessage message)
            {
                Bus.SendLocal(ModifyPublish);
            }
        }

        public class PublishingHandler<TPublish> : IHandleMessages<TestMessage>
            where TPublish : IMessage
        {
            public IBus Bus { get; set; }
            public Action<TPublish> ModifyPublish { get; set; }

            public void Handle(TestMessage message)
            {
                Bus.Publish(ModifyPublish);
            }
        }

        public class PublishingHandler<TPublish1, TPublish2> : IHandleMessages<TestMessage>
            where TPublish1 : IMessage
            where TPublish2 : IMessage
        {
            public IBus Bus { get; set; }
            public Action<TPublish1> ModifyPublish1 { get; set; }
            public Action<TPublish2> ModifyPublish2 { get; set; }

            public void Handle(TestMessage message)
            {
                Bus.Publish(ModifyPublish1);
                Bus.Publish(ModifyPublish2);
            }
        }

        public class DoNotContinueDispatchingCurrentMessageToHandlersHandler : IHandleMessages<TestMessage>
        {
            public IBus Bus { get; set; }

            public void Handle(TestMessage message)
            {
                Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
            }
        }

        public class HandleCurrentMessageLaterHandler : IHandleMessages<TestMessage>
        {
            public IBus Bus { get; set; }

            public void Handle(TestMessage message)
            {
                Bus.HandleCurrentMessageLater();
            }
        }

        public class ExplicitInterfaceImplementation : IHandleMessages<TestMessage>
        {

            public bool IsHandled { get; set; }

            void IHandleMessages<TestMessage>.Handle(TestMessage message)
            {
                IsHandled = true;
            }

            // Unit test fails if this is uncommented; seems to me that this should
            // be made to pass, but it looks like a design decision based on commit
            // revision 1210.
            //public void Handle(TestMessage message) {
            //    throw new System.Exception("Shouldn't call this.");
            //}

        }

    }

    public class DataBusMessageHandler : IHandleMessages<MessageWithDataBusProperty>
    {
        public void Handle(MessageWithDataBusProperty message)
        {

        }
    }

    public interface TestMessage : IMessage
    {
    }

    public class MessageWithDataBusProperty : IMessage
    {
        public DataBusProperty<byte[]> ALargeByteArray { get; set; }
    }

    public class TestMessageWithPropertiesHandler : IHandleMessages<TestMessageWithProperties>
    {
        public IBus Bus { get; set; }
        public string ReceivedDummyValue;
        public string AssignedMessageId;

        public void Handle(TestMessageWithProperties message)
        {
            ReceivedDummyValue = message.Dummy;
            AssignedMessageId = Bus.CurrentMessageContext.Id;
        }
    }

    public class TestMessageWithProperties : IMessage
    {
        public string Dummy { get; set; }
    }
}
