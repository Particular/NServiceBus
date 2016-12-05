namespace NServiceBus.AcceptanceTests.Serialization
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class When_serializing_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void DateTime_properties_should_keep_their_original_timezone_information()
        {
            var expectedDateTime = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Unspecified);
            var expectedDateTimeLocal = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Local);
            var expectedDateTimeUtc = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Utc);
            var expectedDateTimeOffset = new DateTimeOffset(2012, 12, 12, 12, 12, 12, TimeSpan.FromHours(6));
            var expectedDateTimeOffsetLocal = DateTimeOffset.Now;
            var expectedDateTimeOffsetUtc = DateTimeOffset.UtcNow;

            var context = Scenario.Define<Context>()
                .WithEndpoint<DateTimeReceiver>(b => b.When(
                    bus => bus.SendLocal(new DateTimeMessage
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

            Assert.AreEqual(expectedDateTime, context.ReceivedMessage.DateTime);
            Assert.AreEqual(expectedDateTimeLocal, context.ReceivedMessage.DateTimeLocal);
            Assert.AreEqual(expectedDateTimeUtc, context.ReceivedMessage.DateTimeUtc);
            Assert.AreEqual(expectedDateTimeOffset, context.ReceivedMessage.DateTimeOffset);
            Assert.AreEqual(expectedDateTimeOffsetLocal, context.ReceivedMessage.DateTimeOffsetLocal);
            Assert.AreEqual(expectedDateTimeOffsetUtc, context.ReceivedMessage.DateTimeOffsetUtc);
            Assert.AreEqual(expectedDateTimeOffsetLocal, context.ReceivedMessage.DateTimeOffsetLocal);
            Assert.AreEqual(expectedDateTimeOffsetLocal.Offset, context.ReceivedMessage.DateTimeOffsetLocal.Offset);
            Assert.AreEqual(expectedDateTimeOffsetUtc, context.ReceivedMessage.DateTimeOffsetUtc);
            Assert.AreEqual(expectedDateTimeOffsetUtc.Offset, context.ReceivedMessage.DateTimeOffsetUtc.Offset);
        }

        class DateTimeReceiver : EndpointConfigurationBuilder
        {
            public DateTimeReceiver()
            {
                EndpointSetup<DefaultServer>(c => { c.UseSerialization<JsonSerializer>(); });
            }

            class DateTimeMessageHandler : IHandleMessages<DateTimeMessage>
            {
                public Context Context { get; set; }

                public void Handle(DateTimeMessage message)
                {
                    Context.ReceivedMessage = message;
                }
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
}
