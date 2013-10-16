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
                MessageConventionExtensions.IsMessageTypeAction = t =>
                {
                    timesCalled++;
                    return false;
                };

                MessageConventionExtensions.IsMessage(this);
                Assert.AreEqual(1, timesCalled);

                MessageConventionExtensions.IsMessage(this);
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
                MessageConventionExtensions.IsEventTypeAction = t =>
                {
                    timesCalled++;
                    return false;
                };

                MessageConventionExtensions.IsEvent(this);
                Assert.AreEqual(1, timesCalled);

                MessageConventionExtensions.IsEvent(this);
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
                MessageConventionExtensions.IsCommandTypeAction = t =>
                {
                    timesCalled++;
                    return false;
                };

                MessageConventionExtensions.IsCommand(this);
                Assert.AreEqual(1, timesCalled);

                MessageConventionExtensions.IsCommand(this);
                Assert.AreEqual(1, timesCalled);
            }
        }
    }
}

namespace NServiceBus.Core.Tests.Conventions.NServiceBus.Config.UnitTests
{
    using System;
    using NUnit.Framework;

    public class MessageConventionTestBase
    {
        Func<Type, bool> IsEventTypeAction;
        Func<Type, bool> IsCommandTypeAction;
        Func<Type, bool> IsMessageTypeAction;

        [SetUp]
        public void SetUp()
        {
            IsEventTypeAction = MessageConventionExtensions.IsEventTypeAction;
            IsCommandTypeAction = MessageConventionExtensions.IsCommandTypeAction;
            IsMessageTypeAction = MessageConventionExtensions.IsMessageTypeAction;

        }

        [TearDown]
        public void TearDown()
        {
            MessageConventionExtensions.IsEventTypeAction = IsEventTypeAction;
            MessageConventionExtensions.IsCommandTypeAction = IsCommandTypeAction;
            MessageConventionExtensions.IsMessageTypeAction = IsMessageTypeAction;

        }
    }
}
