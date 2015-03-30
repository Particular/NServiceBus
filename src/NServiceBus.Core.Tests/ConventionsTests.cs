namespace NServiceBus.Core.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class ConventionsTests
    {
        [Test]
        public void Should_use_TimeToBeReceived_from_bottom_of_tree()
        {
            var conventions = new NServiceBus.Conventions();
            var timeToBeReceivedAction = conventions.GetTimeToBeReceived(typeof(InheritedClassWithAttribute));
            Assert.AreEqual(TimeSpan.FromSeconds(2), timeToBeReceivedAction);
        }

        [Test]
        public void Should_use_inherited_TimeToBeReceived()
        {
            var conventions = new NServiceBus.Conventions();
            var timeToBeReceivedAction = conventions.GetTimeToBeReceived(typeof(InheritedClassWithNoAttribute));
            Assert.AreEqual(TimeSpan.FromSeconds(1), timeToBeReceivedAction);
        }

        [TimeToBeReceivedAttribute("00:00:01")]
        class BaseClass
        {
        }

        [TimeToBeReceivedAttribute("00:00:02")]
        class InheritedClassWithAttribute : BaseClass
        {

        }

        class InheritedClassWithNoAttribute : BaseClass
        {

        }

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

        [Test]
        public void IsResponseType_should_return_true_for_IResponse()
        {
            var conventions = new NServiceBus.Conventions();
            Assert.IsTrue(conventions.IsResponseType(typeof(MyResponse)));
        }

        public class MyResponse : IResponse
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
            public void IsExpressMessageType_should_return_false_for_NServiceBus_types()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsExpressMessageAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly
                };
                Assert.IsFalse(conventions.IsExpressMessageType(typeof(NServiceBus.Conventions)));
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

            [Test]
            public void IsResponseType_should_return_false_for_NServiceBus_types()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsResponseTypeAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly
                };
                Assert.IsFalse(conventions.IsResponseType(typeof(NServiceBus.Conventions)));
            }

            [Test]
            public void IsExpressType_should_return_true_for_matching_type()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsExpressMessageAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly ||
                                               t == typeof(MyConventionExpress)
                };
                Assert.IsTrue(conventions.IsExpressMessageType(typeof(MyConventionExpress)));
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

            [Test]
            public void IsResponseType_should_return_true_for_matching_type()
            {
                var conventions = new NServiceBus.Conventions
                {
                    IsResponseTypeAction = t => t.Assembly == typeof(NServiceBus.Conventions).Assembly ||
                                               t == typeof(MyConventionResponse)
                };
                Assert.IsTrue(conventions.IsResponseTypeAction(typeof(MyConventionResponse)));
            }

            public class MyConventionResponse
            {
            }
        }
    }
}