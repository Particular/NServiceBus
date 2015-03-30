namespace NServiceBus.Unicast.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class MessagingBestPracticesTests
    {
        [TestFixture]
        public class When_sending
        {
            [Test]
            [Ignore("Should we enable this. This is a behavior breaking change")]
            public void Should_throw_for_message()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForSend(typeof(MyMessage), new Conventions()));
                Assert.AreEqual("Send is neither supported for Messages, Replies nor Events. Commands should be sent to their logical owner using bus.Send, Replies should be Replied with bus.Reply and Events should be Published with bus.Publish.", invalidOperationException.Message);
            }

            [Test]
            public void Should_throw_for_event()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForSend(typeof(MyEvent), new Conventions()));
                Assert.AreEqual("Send is neither supported for Messages, Replies nor Events. Commands should be sent to their logical owner using bus.Send, Replies should be Replied with bus.Reply and Events should be Published with bus.Publish.", invalidOperationException.Message);
            }

            [Test]
            public void Should_throw_for_replies()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForSend(typeof(MyResponse), new Conventions()));
                Assert.AreEqual("Send is neither supported for Messages, Replies nor Events. Commands should be sent to their logical owner using bus.Send, Replies should be Replied with bus.Reply and Events should be Published with bus.Publish.", invalidOperationException.Message);
            }

            [Test]
            public void Should_not_throw_for_commands()
            {
                Assert.DoesNotThrow(() => MessagingBestPractices.AssertIsValidForSend(typeof(MyCommand), new Conventions()));
            }
        }

        [TestFixture]
        public class When_replying
        {
            [Test]
            public void Should_throw_for_command()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForReply(typeof(MyCommand), new Conventions()));
                Assert.AreEqual("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and Events should be Published with bus.Publish.", invalidOperationException.Message);
            }

            [Test]
            public void Should_throw_for_event()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForReply(typeof(MyEvent), new Conventions()));
                Assert.AreEqual("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and Events should be Published with bus.Publish.", invalidOperationException.Message);
            }

            [Test]
            public void Should_not_throw_for_message()
            {
                Assert.DoesNotThrow(() => MessagingBestPractices.AssertIsValidForReply(typeof(MyMessage), new Conventions()));
            }

            [Test]
            public void Should_not_throw_for_replies()
            {
                Assert.DoesNotThrow(() => MessagingBestPractices.AssertIsValidForReply(typeof(MyResponse), new Conventions()));
            }
        }

        [TestFixture]
        public class When_pubsub
        {
            [Test]
            public void Should_throw_for_command()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForPubSub(typeof(MyCommand), new Conventions()));
                Assert.AreEqual("Pub/Sub is not supported for Commands. They should be sent direct to their logical owner.", invalidOperationException.Message);
            }

            [Test]
            public void Should_not_throw_for_event()
            {
                Assert.DoesNotThrow(() => MessagingBestPractices.AssertIsValidForPubSub(typeof(MyEvent), new Conventions()));
            }

            [Test]
            public void Should_not_throw_for_message()
            {
                Assert.DoesNotThrow(() => MessagingBestPractices.AssertIsValidForPubSub(typeof(MyMessage), new Conventions()));
            }

            [Test]
            public void Should_throw_for_replies()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForPubSub(typeof(MyResponse), new Conventions()));
                Assert.AreEqual("Pub/Sub is not supported for Replies. They should be replied to their logical owner.", invalidOperationException.Message);
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
        public class MyResponse : IResponse
        {

        }
    }
}