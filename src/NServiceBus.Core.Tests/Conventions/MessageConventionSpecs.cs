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
                conventions = new Conventions
                {
                    IsMessageTypeAction= t =>
                    {
                        timesCalled++;
                        return false;
                    }
                };

                conventions.IsMessageType(GetType());
                Assert.AreEqual(1, timesCalled);

                conventions.IsMessageType(GetType());
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
                conventions = new Conventions
                {
                    IsEventTypeAction = t =>
                    {
                        timesCalled++;
                        return false;
                    }
                };

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
                conventions = new Conventions
                {
                    IsCommandTypeAction = t =>
                    {
                        timesCalled++;
                        return false;
                    }
                };

                conventions.IsCommandType(GetType());
                Assert.AreEqual(1, timesCalled);

                conventions.IsCommandType(GetType());
                Assert.AreEqual(1, timesCalled);
            }
        }

        [TestFixture]
        public class When_applying_message_conventions_to_replies : MessageConventionTestBase
        {
            [Test]
            public void Should_cache_the_message_convention()
            {
                var timesCalled = 0;
                conventions = new Conventions
                {
                    IsResponseTypeAction = t =>
                    {
                        timesCalled++;
                        return false;
                    }
                };

                conventions.IsResponseType(GetType());
                Assert.AreEqual(1, timesCalled);

                conventions.IsResponseType(GetType());
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
