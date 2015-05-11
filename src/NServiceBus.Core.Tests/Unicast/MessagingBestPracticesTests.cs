namespace NServiceBus.Unicast.Tests
{
    using System;
    using NServiceBus.MessagingBestPractices;
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
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() => 
                    new Validations(new Conventions()).AssertIsValidForReply(typeof(MyCommand)));
                Assert.AreEqual("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and Events should be Published with bus.Publish.", invalidOperationException.Message);
            }

            [Test]
            public void Should_throw_for_event()
            {
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() =>
                    new Validations(new Conventions()).AssertIsValidForReply(typeof(MyEvent)));
                Assert.AreEqual("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and Events should be Published with bus.Publish.", invalidOperationException.Message);
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
                var invalidOperationException = Assert.Throws<InvalidOperationException>(() =>
                    new Validations(new Conventions()).AssertIsValidForPubSub(typeof(MyCommand)));
                Assert.AreEqual("Pub/Sub is not supported for Commands. They should be sent direct to their logical owner.", invalidOperationException.Message);
            }

            [Test]
            public void Should_not_throw_for_event()
            {
                new Validations(new Conventions()).AssertIsValidForPubSub(typeof(MyEvent));
                //TODO: verify log
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
    }
}