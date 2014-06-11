namespace NServiceBus.Core.Tests.Conventions
{
    namespace NServiceBus.Config.UnitTests
    {
        using NUnit.Framework;

        [TestFixture]
        public class When_applying_message_conventions_to_messages : MessageConventionTestBase
        {
            [Test]
            public void Should_cache_the_message_convention()
            {
                var timesCalled = 0;
                conventions.IsMessageTypeAction = t =>
                {
                    timesCalled++;
                    return false;
                };

                conventions.IsMessage(this);
                Assert.AreEqual(1, timesCalled);

                conventions.IsMessage(this);
                Assert.AreEqual(1, timesCalled);
            }
        }

        [TestFixture]
        public class When_applying_message_conventions_to_events:MessageConventionTestBase
        {
            [Test]
            public void Should_cache_the_message_convention()
            {
                var timesCalled = 0;
                conventions.IsEventTypeAction = t =>
                {
                    timesCalled++;
                    return false;
                };

                conventions.IsEvent(this);
                Assert.AreEqual(1, timesCalled);

                conventions.IsEvent(this);
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
                conventions.IsCommandTypeAction = t =>
                {
                    timesCalled++;
                    return false;
                };

                conventions.IsCommand(this);
                Assert.AreEqual(1, timesCalled);

                conventions.IsCommand(this);
                Assert.AreEqual(1, timesCalled);
            }
        }
    }
}

namespace NServiceBus.Core.Tests.Conventions.NServiceBus.Config.UnitTests
{
    using NUnit.Framework;
    using Conventions = global::NServiceBus.Conventions;

    public class MessageConventionTestBase
    {
        protected Conventions conventions;

        [SetUp]
        public void SetUp()
        {
            conventions = new Conventions();
        }
    }
}
