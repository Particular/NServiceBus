namespace NServiceBus.Unicast.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class MessagingBestPracticesTests
    {
        [TestFixture]
        public class When_replying
        {
            [Test]
            public void Should_throw_for_command()
            {
                var invalidOperationException = Assert.Throws<Exception>(() =>
                        new Validations(new Conventions()).AssertIsValidForReply(typeof(MyCommand)));
                Assert.AreEqual($"Best practice violation for message type '{typeof(MyCommand).FullName}'. Reply is not supported for commands or events. Commands should be sent to their logical owner. Events should be published.", invalidOperationException.Message);
            }

            [Test]
            public void Should_throw_for_event()
            {
                var invalidOperationException = Assert.Throws<Exception>(() =>
                        new Validations(new Conventions()).AssertIsValidForReply(typeof(MyEvent)));
                Assert.AreEqual($"Best practice violation for message type '{typeof(MyEvent).FullName}'. Reply is not supported for commands or events. Commands should be sent to their logical owner. Events should be published.", invalidOperationException.Message);
            }

            [Test]
            public void Should_not_throw_for_message()
            {
                new Validations(new Conventions())
                    .AssertIsValidForReply(typeof(MyMessage));
            }
        }

        [TestFixture]
        public class When_pubsub
        {
            [Test]
            public void Should_throw_for_command()
            {
                var invalidOperationException = Assert.Throws<Exception>(() =>
                        new Validations(new Conventions()).AssertIsValidForPubSub(typeof(MyCommand)));
                Assert.AreEqual($"Best practice violation for message type '{typeof(MyCommand).FullName}'. Pub/sub is not supported for commands, so they should be be sent to their logical owner instead.", invalidOperationException.Message);
            }

            [Test]
            public void Should_not_throw_for_event()
            {
                new Validations(new Conventions()).AssertIsValidForPubSub(typeof(MyEvent));
            }

            [Test]
            public void Should_not_throw_for_message()
            {
                new Validations(new Conventions()).AssertIsValidForPubSub(typeof(MyMessage));
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class MyCommand : ICommand
        {
        }

        public class MyEvent : IEvent
        {
        }

        [TimeToBeReceived("00:00:01")]
        public class MyDeferredMessage : IMessage
        {
        }
    }
}
