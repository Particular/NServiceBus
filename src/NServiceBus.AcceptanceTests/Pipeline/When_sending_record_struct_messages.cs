namespace NServiceBus.AcceptanceTests.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_record_struct_messages : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_process_record_message()
    {
        string expectedText = Guid.NewGuid().ToString();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<RecordHandlingEndpoint>(e =>
                e.When(s => s.SendLocal(new RecordStructMessage { SomeText = expectedText })))
            .Done(c => c.ReceivedText != null)
            .Run();

        Assert.That(context.ReceivedText, Is.EqualTo(expectedText));
    }

    [Test]
    public async Task Should_process_readonly_record_message()
    {
        string expectedText = Guid.NewGuid().ToString();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<RecordHandlingEndpoint>(e =>
                e.When(s => s.SendLocal(new ReadonlyRecordStructMessage(expectedText))))
            .Done(c => c.ReceivedText != null)
            .Run();

        Assert.That(context.ReceivedText, Is.EqualTo(expectedText));
    }

    public class Context : ScenarioContext
    {
        public string ReceivedText { get; set; }
    }

    public class RecordHandlingEndpoint : EndpointConfigurationBuilder
    {
        public RecordHandlingEndpoint() => EndpointSetup<DefaultServer>();

        public class RecordMessageHandler(Context testContext) :
            IHandleMessages<RecordStructMessage>,
            IHandleMessages<ReadonlyRecordStructMessage>
        {
            public Task Handle(RecordStructMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedText = message.SomeText;
                return Task.CompletedTask;
            }

            public Task Handle(ReadonlyRecordStructMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedText = message.SomeText;
                return Task.CompletedTask;
            }
        }
    }

    public record struct RecordStructMessage : IMessage
    {
        public string SomeText { get; set; }
    }

    public readonly record struct ReadonlyRecordStructMessage(string SomeText) : IMessage
    {
    }
}