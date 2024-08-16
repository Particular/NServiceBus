namespace NServiceBus.Core.Tests;

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
        Assert.That(timesCalled, Is.EqualTo(1));

        conventions.IsMessageType(GetType());
        Assert.That(timesCalled, Is.EqualTo(1));
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
        Assert.That(timesCalled, Is.EqualTo(1));

        conventions.IsEventType(GetType());
        Assert.That(timesCalled, Is.EqualTo(1));
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
        Assert.That(timesCalled, Is.EqualTo(1));

        conventions.IsCommandType(GetType());
        Assert.That(timesCalled, Is.EqualTo(1));
    }
}

public class MessageConventionTestBase
{
    protected Conventions conventions;
}