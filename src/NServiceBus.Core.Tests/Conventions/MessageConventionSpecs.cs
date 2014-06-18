namespace NServiceBus.Core.Tests.Conventions
{
    namespace NServiceBus.Config.UnitTests
    {
        using NUnit.Framework;
        using Conventions = global::NServiceBus.Conventions;

        [TestFixture]
        public class When_applying_message_conventions_to_messages : MessageConventionTestBase
        {
            [Test]
            public void Should_cache_the_message_convention()
            {
                var timesCalled = 0;
                conventions = new Conventions(isMessageTypeAction: t =>
                {
                    timesCalled++;
                    return false;
                });

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
                conventions = new Conventions(isEventTypeAction: t =>
                {
                    timesCalled++;
                    return false;
                });

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
                conventions = new Conventions(isCommandTypeAction: t =>
                {
                    timesCalled++;
                    return false;
                });

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
    using Conventions = global::NServiceBus.Conventions;

    public class MessageConventionTestBase
    {
        protected Conventions conventions;
    }
}
