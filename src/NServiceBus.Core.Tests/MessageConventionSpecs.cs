namespace NServiceBus.Core.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class When_applying_message_conventions_to_messages : MessageConventionTestBase
    {
        [Test]
        public void Should_cache_the_message_convention()
        {
            var timesCalled = 0;
            conventions = new Conventions();

            conventions.DefineMessageTypeConvention(t =>
            {
                timesCalled++;
                return false;
            });
            conventions.IsMessageType(GetType());
            Assert.AreEqual(1, timesCalled);

            conventions.IsMessageType(GetType());
            Assert.AreEqual(1, timesCalled);
        }
    }

    [TestFixture]
    public class When_applying_message_conventions_to_events : MessageConventionTestBase
    {
        [Test]
        public void Should_cache_the_message_convention()
        {
            var timesCalled = 0;
            conventions = new Conventions();

            conventions.DefineEventTypeConventions(t =>
            {
                timesCalled++;
                return false;
            });

            conventions.IsEventType(GetType());
            Assert.AreEqual(1, timesCalled);

            conventions.IsEventType(GetType());
            Assert.AreEqual(1, timesCalled);
        }
    }

    [TestFixture]
    public class When_applying_message_conventions_to_commands : MessageConventionTestBase
    {
        [Test]
        public void Should_cache_the_message_convention()
        {
            var timesCalled = 0;
            conventions = new Conventions();

            conventions.DefineCommandTypeConventions(t =>
            {
                timesCalled++;
                return false;
            });

            conventions.IsCommandType(GetType());
            Assert.AreEqual(1, timesCalled);

            conventions.IsCommandType(GetType());
            Assert.AreEqual(1, timesCalled);
        }
    }

    public class MessageConventionTestBase
    {
        protected Conventions conventions;
    }
}