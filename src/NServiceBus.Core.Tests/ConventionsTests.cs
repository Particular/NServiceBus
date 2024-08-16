namespace NServiceBus.Core.Tests;

using NUnit.Framework;
using System;

[TestFixture]
public class ConventionsTests
{
    [Test]
    public void IsMessageType_should_return_false_for_unknown_type()
    {
        var conventions = new Conventions();
        Assert.That(conventions.IsMessageType(typeof(NoAMessage)), Is.False);
    }

    public class NoAMessage
    {

    }

    [Test]
    public void IsMessageType_should_return_true_for_IMessage()
    {
        var conventions = new Conventions();
        Assert.That(conventions.IsMessageType(typeof(MyMessage)), Is.True);
    }

    public class MyMessage : IMessage
    {

    }

    [Test]
    public void IsMessageType_should_return_true_for_ICommand()
    {
        var conventions = new Conventions();
        Assert.That(conventions.IsMessageType(typeof(MyCommand)), Is.True);
    }

    public class MyCommand : ICommand
    {

    }

    [Test]
    public void IsMessageType_should_return_true_for_IEvent()
    {
        var conventions = new Conventions();
        Assert.That(conventions.IsMessageType(typeof(MyEvent)), Is.True);
    }

    public class MyEvent : IEvent
    {

    }

    [Test]
    public void IsMessageType_should_return_true_for_systemMessage()
    {
        var conventions = new Conventions();
        conventions.AddSystemMessagesConventions(type => type == typeof(MySystemMessage));
        Assert.That(conventions.IsMessageType(typeof(MySystemMessage)), Is.True);
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
            Assert.That(conventions.IsCommandType(typeof(Conventions)), Is.False);
        }

        [Test]
        public void IsMessageType_should_return_false_for_NServiceBus_types()
        {
            var conventions = new Conventions();

            conventions.DefineMessageTypeConvention(t => t.Assembly == typeof(Conventions).Assembly);
            Assert.That(conventions.IsMessageType(typeof(Conventions)), Is.False);
        }

        [Test]
        public void IsEventType_should_return_false_for_NServiceBus_types()
        {
            var conventions = new Conventions();
            conventions.DefineEventTypeConventions(t => t.Assembly == typeof(Conventions).Assembly);
            Assert.That(conventions.IsEventType(typeof(Conventions)), Is.False);
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
            Assert.That(conventions.IsCommandType(typeof(MyConventionCommand)), Is.True);
        }

        public class MyConventionCommand
        {
        }

        [Test]
        public void IsMessageType_should_return_true_for_matching_type()
        {
            var conventions = new Conventions();

            conventions.DefineMessageTypeConvention(t => t.Assembly == typeof(Conventions).Assembly || t == typeof(MyConventionMessage));
            Assert.That(conventions.IsMessageType(typeof(MyConventionMessage)), Is.True);
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
            Assert.That(conventions.IsEventType(typeof(MyConventionEvent)), Is.True);
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
            Assert.That(conventions.IsCommandType(typeof(MyConventionCommand)), Is.True);
        }

        [Test]
        public void IsEventType_should_return_true_for_matching_type()
        {
            var conventions = new Conventions();
            conventions.Add(new MyConvention());
            Assert.That(conventions.IsEventType(typeof(MyConventionEvent)), Is.True);
        }

        [Test]
        public void IsMessageType_should_return_true_for_matching_type()
        {
            var conventions = new Conventions();
            conventions.Add(new MyConvention());
            Assert.That(conventions.IsMessageType(typeof(MyConventionMessage)), Is.True);
        }

        [Test]
        public void IsCommandType_should_return_true_for_default_convention()
        {
            var conventions = new Conventions();
            conventions.Add(new MyConvention());
            Assert.That(conventions.IsCommandType(typeof(DefaultConventionCommand)), Is.True);
        }

        [Test]
        public void IsEventType_should_return_true_for_default_convention()
        {
            var conventions = new Conventions();
            conventions.Add(new MyConvention());
            Assert.That(conventions.IsEventType(typeof(DefaultConventionEvent)), Is.True);
        }

        [Test]
        public void IsMessageType_should_return_true_for_default_convention()
        {
            var conventions = new Conventions();
            conventions.Add(new MyConvention());
            Assert.That(conventions.IsMessageType(typeof(DefaultConventionMessage)), Is.True);
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