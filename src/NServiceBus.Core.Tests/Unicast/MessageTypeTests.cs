namespace NServiceBus.Unicast.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class MessageTypeTests
    {
        [Test]
        public void Should_parse_types()
        {
            var messageType = new Subscriptions.MessageType(typeof(TestMessage));

            Assert.AreEqual(messageType.TypeName, typeof(TestMessage).FullName);
            Assert.AreEqual(messageType.Version, typeof(TestMessage).Assembly.GetName().Version);
        }

        [Test]
        public void Should_parse_AssemblyQualifiedName()
        {
            var messageType = new Subscriptions.MessageType(typeof(TestMessage).AssemblyQualifiedName);

            Assert.AreEqual(messageType.TypeName, typeof(TestMessage).FullName);
            Assert.AreEqual(messageType.Version, typeof(TestMessage).Assembly.GetName().Version);
        }

        [Test]
        public void Should_parse_version_strings()
        {
            var messageType = new Subscriptions.MessageType("TestMessage", "1.2.3.4");

            Assert.AreEqual(messageType.TypeName, "TestMessage");
            Assert.AreEqual(messageType.Version, new Version(1, 2, 3, 4));
        }


        class TestMessage
        {

        }
    }
}