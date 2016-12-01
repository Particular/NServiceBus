namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_serializing_a_message:NServiceBusAcceptanceTest
    {
        [Test]
        public async Task DateTime_properties_should_keep_their_original_timezone_information()
        {
            var expectedDateTime = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Unspecified);
            var expectedDateTimeLocal = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Local);
            var expectedDateTimeUtc = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Utc);
            var expectedDateTimeOffset = new DateTimeOffset(2012, 12, 12, 12, 12, 12, TimeSpan.FromHours(6));
            var expectedDateTimeOffsetLocal = DateTimeOffset.Now;
            var expectedDateTimeOffsetUtc = DateTimeOffset.UtcNow;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<DateTimeSender>(b => b.When(
                    (session, c) =>
                    {
                        return session.Send(new DateTimeMessage
                        {
                            DateTime = expectedDateTime,
                            DateTimeLocal = expectedDateTimeLocal,
                            DateTimeUtc = expectedDateTimeUtc,
                            DateTimeOffset = expectedDateTimeOffset,
                            DateTimeOffsetLocal = expectedDateTimeOffsetLocal,
                            DateTimeOffsetUtc = expectedDateTimeOffsetUtc
                        });
                    }))
                .WithEndpoint<DateTimeReceiver>()
                .Done(c=>c.HandlerGotTheRequest = true)
                .Run();

            Assert.True(context.HandlerGotTheRequest);
            Assert.AreEqual(expectedDateTime,context.MessageDateTime);
            Assert.AreEqual(expectedDateTimeLocal,context.MessageDateTimeLocal);
            Assert.AreEqual(expectedDateTimeUtc,context.MessageDateTimeUtc);
            Assert.AreEqual(expectedDateTimeOffset,context.MessageDateTimeOffset);
            Assert.AreEqual(expectedDateTimeOffsetLocal,context.MessageDateTimeOffsetLocal);
            Assert.AreEqual(expectedDateTimeOffsetUtc,context.MessageDateTimeOffsetUtc);

        }
        class DateTimeSender : EndpointConfigurationBuilder
        {
            public DateTimeSender()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.UseSerialization<JsonSerializer>();
                    })
                    .AddMapping<DateTimeMessage>(typeof(DateTimeReceiver));
            }
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

                public Task Handle(DateTimeMessage request, IMessageHandlerContext context)
                {
                    Context.MessageDateTime = request.DateTime;
                    Context.MessageDateTimeLocal = request.DateTimeLocal;
                    Context.MessageDateTimeUtc = request.DateTimeUtc;
                    Context.MessageDateTimeOffset = request.DateTimeOffset;
                    Context.MessageDateTimeOffsetLocal = request.DateTimeOffsetLocal;
                    Context.MessageDateTimeOffsetUtc = request.DateTimeOffsetUtc;

                    return Task.FromResult(0);
                }
            }
        }

        public class DateTimeMessage:IMessage
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
            public DateTime MessageDateTime  { get; set; }
            public DateTime MessageDateTimeLocal  { get; set; }
            public DateTime MessageDateTimeUtc  { get; set; }
            public DateTimeOffset MessageDateTimeOffset { get; set; }
            public DateTimeOffset MessageDateTimeOffsetLocal  { get; set; }
            public DateTimeOffset MessageDateTimeOffsetUtc  { get; set; }
            public bool HandlerGotTheRequest { get; set; }
        }
    }
}