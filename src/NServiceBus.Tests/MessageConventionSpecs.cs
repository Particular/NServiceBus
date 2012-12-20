namespace NServiceBus.Core.Tests
{
    namespace NServiceBus.Config.UnitTests
    {
        using System;
        using System.Collections.Generic;
        using System.Diagnostics;
        using NUnit.Framework;

        [TestFixture]
        public class When_applying_message_conventions_to_messages
        {
            [Test]
            public void Should_cache_the_message_convention()
            {
                var timesCalled = 0;
                MessageConventionExtensions.IsMessageTypeAction = (t) =>
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
        public class When_applying_message_conventions_to_events
        {
            [Test]
            public void Should_cache_the_message_convention()
            {
                var timesCalled = 0;
                MessageConventionExtensions.IsEventTypeAction = (t) =>
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
        public class When_applying_message_conventions_to_commands
        {
            [Test]
            public void Should_cache_the_message_convention()
            {
                var timesCalled = 0;
                MessageConventionExtensions.IsCommandTypeAction = (t) =>
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
