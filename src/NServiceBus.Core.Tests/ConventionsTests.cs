namespace NServiceBus.Core.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class ConventionsTests
    {
      
        [Test]
        public void IsMessageType_should_return_false_for_unknown_type()
        {
            var conventions = new NServiceBus.Conventions();
            Assert.IsFalse(conventions.IsMessageType(typeof(NoAMessage)));
        }

        public class NoAMessage
        {

        }

        [Test]
        public void IsMessageType_should_return_true_for_IMessage()
        {
            var conventions = new NServiceBus.Conventions();
            Assert.IsTrue(conventions.IsMessageType(typeof(MyMessage)));
        }

        public class MyMessage : IMessage
        {

        }

        [Test]
        public void IsMessageType_should_return_true_for_ICommand()
        {
            var conventions = new NServiceBus.Conventions();
            Assert.IsTrue(conventions.IsMessageType(typeof(MyCommand)));
        }

        public class MyCommand : ICommand
        {

        }

        [Test]
        public void IsMessageType_should_return_true_for_IEvent()
        {
            var conventions = new NServiceBus.Conventions();
            Assert.IsTrue(conventions.IsMessageType(typeof(MyEvent)));
        }

        public class MyEvent : IEvent
        {

        }

        [Test]
        public void IsMessageType_should_return_true_for_systemMessage()
        {
            var conventions = new NServiceBus.Conventions();
            conventions.AddSystemMessagesConventions(type => type == typeof(MySystemMessage));
            Assert.IsTrue(conventions.IsMessageType(typeof(MySystemMessage)));
        }

        public class MySystemMessage
        {

        }


        [TestFixture]
        public class When_using_a_greedy_convention_that_overlaps_with_NServiceBus
        {
            [Test]
            public void IsCommandType_should_return_false_for_NServiceBus_types()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsCommandTypeAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly
                };
                Assert.IsFalse(conventions.IsCommandType(typeof(NServiceBus.Conventions)));
            }

        
            [Test]
            public void IsMessageType_should_return_false_for_NServiceBus_types()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsMessageTypeAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly
                };
                Assert.IsFalse(conventions.IsMessageType(typeof(NServiceBus.Conventions)));
            }

            [Test]
            public void IsEventType_should_return_false_for_NServiceBus_types()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsEventTypeAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly
                };
                Assert.IsFalse(conventions.IsEventType(typeof(NServiceBus.Conventions)));
            }

            public class MyConventionExpress
            {
            }

            [Test]
            public void IsCommandType_should_return_true_for_matching_type()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsCommandTypeAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly ||
                                               t == typeof(MyConventionCommand)
                };
                Assert.IsTrue(conventions.IsCommandType(typeof(MyConventionCommand)));
            }

            public class MyConventionCommand
            {
            }

            [Test]
            public void IsMessageType_should_return_true_for_matching_type()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsMessageTypeAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly ||
                                               t == typeof(MyConventionMessage)
                };
                Assert.IsTrue(conventions.IsMessageType(typeof(MyConventionMessage)));
            }

            public class MyConventionMessage
            {
            }

            [Test]
            public void IsEventType_should_return_true_for_matching_type()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsEventTypeAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly ||
                                               t == typeof(MyConventionEvent)
                };
                Assert.IsTrue(conventions.IsEventType(typeof(MyConventionEvent)));
            }

            public class MyConventionEvent
            {
            }
        }
    }
}