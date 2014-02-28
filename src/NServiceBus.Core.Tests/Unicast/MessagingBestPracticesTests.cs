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
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForReply(new[] { new MyCommand() }));
                Assert.AreEqual("Reply is not supported for Commands. Commands should be sent to their logical owner using bus.Send and bus.",invalidOperationException.Message);
            }

            [Test]
            public void Should_throw_for_event()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForReply(new[] { new MyEvent() }));
                Assert.AreEqual("Reply is not supported for Events. Events should be Published with bus.Publish.",invalidOperationException.Message);
            }

            [Test]
            public void Should_not_throw_for_message()
            {
                MessagingBestPractices.AssertIsValidForReply(new[] { new MyMessage() });
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
        }

        [TestFixture]
        public class When_pubsub
        {
            [Test]
            public void Should_throw_for_command()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => MessagingBestPractices.AssertIsValidForPubSub(typeof(MyCommand)));
                Assert.AreEqual("Pub/Sub is not supported for Commands. They should be be sent direct to their logical owner.", invalidOperationException.Message);
            }

            [Test]
            public void Should_not_throw_for_event()
            {
                MessagingBestPractices.AssertIsValidForPubSub(typeof(MyEvent));
                //TODO: verify log
            }

            [Test]
            public void Should_not_throw_for_message()
            {
                MessagingBestPractices.AssertIsValidForPubSub(typeof(MyMessage));
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
        }
    }
}