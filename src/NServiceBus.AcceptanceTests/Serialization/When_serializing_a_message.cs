﻿namespace NServiceBus.AcceptanceTests.Serialization;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_serializing_a_message : NServiceBusAcceptanceTest
{
    [Test]
    public async Task DateTime_properties_should_keep_their_original_timezone_information()
    {
        var expectedDateTime = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Unspecified);
        var expectedDateTimeLocal = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Local);
        var expectedDateTimeUtc = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Utc);
        var expectedDateTimeOffset = new DateTimeOffset(2012, 12, 12, 12, 12, 12, TimeSpan.FromHours(6));
#pragma warning disable PS0023 // Use DateTime.UtcNow or DateTimeOffset.UtcNow
        var expectedDateTimeOffsetLocal = DateTimeOffset.Now;
#pragma warning restore PS0023 // Use DateTime.UtcNow or DateTimeOffset.UtcNow
        var expectedDateTimeOffsetUtc = DateTimeOffset.UtcNow;

        var context = await Scenario.Define<Context>()
            .WithEndpoint<DateTimeReceiver>(b => b.When(
                (session, c) => session.SendLocal(new DateTimeMessage
                {
                    DateTime = expectedDateTime,
                    DateTimeLocal = expectedDateTimeLocal,
                    DateTimeUtc = expectedDateTimeUtc,
                    DateTimeOffset = expectedDateTimeOffset,
                    DateTimeOffsetLocal = expectedDateTimeOffsetLocal,
                    DateTimeOffsetUtc = expectedDateTimeOffsetUtc
                })))
            .Done(c => c.ReceivedMessage != null)
            .Run();

        Assert.That(context.ReceivedMessage.DateTime, Is.EqualTo(expectedDateTime));
        Assert.That(context.ReceivedMessage.DateTimeLocal, Is.EqualTo(expectedDateTimeLocal));
        Assert.That(context.ReceivedMessage.DateTimeUtc, Is.EqualTo(expectedDateTimeUtc));
        Assert.That(context.ReceivedMessage.DateTimeOffset, Is.EqualTo(expectedDateTimeOffset));
        Assert.That(context.ReceivedMessage.DateTimeOffsetLocal, Is.EqualTo(expectedDateTimeOffsetLocal));
        Assert.That(context.ReceivedMessage.DateTimeOffsetUtc, Is.EqualTo(expectedDateTimeOffsetUtc));
        Assert.That(context.ReceivedMessage.DateTimeOffsetLocal, Is.EqualTo(expectedDateTimeOffsetLocal));
        Assert.That(context.ReceivedMessage.DateTimeOffsetLocal.Offset, Is.EqualTo(expectedDateTimeOffsetLocal.Offset));
        Assert.That(context.ReceivedMessage.DateTimeOffsetUtc, Is.EqualTo(expectedDateTimeOffsetUtc));
        Assert.That(context.ReceivedMessage.DateTimeOffsetUtc.Offset, Is.EqualTo(expectedDateTimeOffsetUtc.Offset));
    }

    class DateTimeReceiver : EndpointConfigurationBuilder
    {
        public DateTimeReceiver()
        {
            EndpointSetup<DefaultServer>();
        }

        class DateTimeMessageHandler : IHandleMessages<DateTimeMessage>
        {
            public DateTimeMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(DateTimeMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedMessage = message;

                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class DateTimeMessage : IMessage
    {
        public DateTime DateTime { get; set; }
        public DateTime DateTimeLocal { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public DateTimeOffset DateTimeOffsetLocal { get; set; }
        public DateTimeOffset DateTimeOffsetUtc { get; set; }
    }

    class Context : ScenarioContext
    {
        public DateTimeMessage ReceivedMessage { get; set; }
    }
}