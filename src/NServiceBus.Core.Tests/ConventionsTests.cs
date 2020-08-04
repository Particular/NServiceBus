namespace NServiceBus.Core.Tests
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class ConventionsTests
    {
        [Test]
        public void IsMessageType_should_return_false_for_unknown_type()
        {
            var conventions = new Conventions();
            Assert.IsFalse(conventions.IsMessageType(typeof(NoAMessage)));
        }

        public class NoAMessage
        {

        }

        [Test]
        public void IsMessageType_should_return_true_for_IMessage()
        {
            var conventions = new Conventions();
            Assert.IsTrue(conventions.IsMessageType(typeof(MyMessage)));
        }

        public class MyMessage : IMessage
        {

        }

        [Test]
        public void IsMessageType_should_return_true_for_ICommand()
        {
            var conventions = new Conventions();
            Assert.IsTrue(conventions.IsMessageType(typeof(MyCommand)));
        }

        public class MyCommand : ICommand
        {

        }

        [Test]
        public void IsMessageType_should_return_true_for_IEvent()
        {
            var conventions = new Conventions();
            Assert.IsTrue(conventions.IsMessageType(typeof(MyEvent)));
        }

        public class MyEvent : IEvent
        {

        }

        [Test]
        public void IsMessageType_should_return_true_for_systemMessage()
        {
            var conventions = new Conventions();
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
                var conventions = new Conventions();
                conventions.DefineCommandTypeConventions(t => t.Assembly == typeof(Conventions).Assembly);
                Assert.IsFalse(conventions.IsCommandType(typeof(Conventions)));
            }

            [Test]
            public void IsMessageType_should_return_false_for_NServiceBus_types()
            {
                var conventions = new Conventions();

                conventions.DefineMessageTypeConvention(t => t.Assembly == typeof(Conventions).Assembly);
                Assert.IsFalse(conventions.IsMessageType(typeof(Conventions)));
            }

            [Test]
            public void IsEventType_should_return_false_for_NServiceBus_types()
            {
                var conventions = new Conventions();
                conventions.DefineEventTypeConventions(t => t.Assembly == typeof(Conventions).Assembly);
                Assert.IsFalse(conventions.IsEventType(typeof(Conventions)));
            }

            public class MyConventionExpress
            {
            }

            [Test]
            public void IsCommandType_should_return_true_for_matching_type()
            {
                var conventions = new Conventions();
                conventions.DefineCommandTypeConventions(t => t.Assembly == typeof(Conventions).Assembly ||
                                               t == typeof(MyConventionCommand));
                Assert.IsTrue(conventions.IsCommandType(typeof(MyConventionCommand)));
            }

            public class MyConventionCommand
            {
            }

            [Test]
            public void IsMessageType_should_return_true_for_matching_type()
            {
                var conventions = new Conventions();

                conventions.DefineMessageTypeConvention(t => t.Assembly == typeof(Conventions).Assembly || t == typeof(MyConventionMessage));
                Assert.IsTrue(conventions.IsMessageType(typeof(MyConventionMessage)));
            }

            public class MyConventionMessage
            {
            }

            [Test]
            public void IsEventType_should_return_true_for_matching_type()
            {
                var conventions = new Conventions();
                conventions.DefineEventTypeConventions(t => t.Assembly == typeof(Conventions).Assembly ||
                                               t == typeof(MyConventionEvent));
                Assert.IsTrue(conventions.IsEventType(typeof(MyConventionEvent)));
            }

            public class MyConventionEvent
            {
            }
        }

        [TestFixture]
        public class When_using_custom_convention
        {
            [Test]
            public void IsCommandType_should_return_true_for_matching_type()
            {
                var conventions = new Conventions();
                conventions.Add(new MyConvention());
                Assert.IsTrue(conventions.IsCommandType(typeof(MyConventionCommand)));
            }

            [Test]
            public void IsEventType_should_return_true_for_matching_type()
            {
                var conventions = new Conventions();
                conventions.Add(new MyConvention());
                Assert.IsTrue(conventions.IsEventType(typeof(MyConventionEvent)));
            }

            [Test]
            public void IsMessageType_should_return_true_for_matching_type()
            {
                var conventions = new Conventions();
                conventions.Add(new MyConvention());
                Assert.IsTrue(conventions.IsMessageType(typeof(MyConventionMessage)));
            }

            [Test]
            public void IsCommandType_should_return_true_for_default_convention()
            {
                var conventions = new Conventions();
                conventions.Add(new MyConvention());
                Assert.IsTrue(conventions.IsCommandType(typeof(DefaultConventionCommand)));
            }

            [Test]
            public void IsEventType_should_return_true_for_default_convention()
            {
                var conventions = new Conventions();
                conventions.Add(new MyConvention());
                Assert.IsTrue(conventions.IsEventType(typeof(DefaultConventionEvent)));
            }

            [Test]
            public void IsMessageType_should_return_true_for_default_convention()
            {
                var conventions = new Conventions();
                conventions.Add(new MyConvention());
                Assert.IsTrue(conventions.IsMessageType(typeof(DefaultConventionMessage)));
            }

            class DefaultConventionCommand : ICommand { }

            class DefaultConventionEvent : IEvent { }

            class DefaultConventionMessage : IMessage { }

            class MyConventionCommand { }

            class MyConventionEvent { }

            class MyConventionMessage { }

            class MyConvention : IMessageConvention
            {
                public string Name => "Test Convention";

                public bool IsCommandType(Type type) => type == typeof(MyConventionCommand);

                public bool IsEventType(Type type) => type == typeof(MyConventionEvent);

                public bool IsMessageType(Type type) => type == typeof(MyConventionMessage);
            }
        }
    }
}