namespace NServiceBus.AcceptanceTests.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_record_class_messages : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_process_record_message()
    {
        string expectedText = Guid.NewGuid().ToString();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<RecordHandlingEndpoint>(e =>
                e.When(s => s.SendLocal(new RecordClassMessage() { SomeText = expectedText })))
            .Done(c => c.ReceivedText != null)
            .Run();

        Assert.That(context.ReceivedText, Is.EqualTo(expectedText));
    }

    [Test]
    public async Task Should_process_positional_record_message()
    {
        string expectedText = Guid.NewGuid().ToString();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<RecordHandlingEndpoint>(e =>
                e.When(s => s.SendLocal(new ReadonlyRecordClassMessage(expectedText))))
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
            IHandleMessages<RecordClassMessage>,
            IHandleMessages<ReadonlyRecordClassMessage>
        {
            public Task Handle(RecordClassMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedText = message.SomeText;
                return Task.CompletedTask;
            }

            public Task Handle(ReadonlyRecordClassMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedText = message.SomeText;
                return Task.CompletedTask;
            }
        }
    }

    //NOTE updated to a class with a paremeterless constructor to avoid issues with serializers that require it - for spike purposes
    public class RecordClassMessage : IMessage
    {
        public string SomeText { get; set; }
    }

    public class ReadonlyRecordClassMessage : IMessage
    {
        public ReadonlyRecordClassMessage()
        {

        }

        public ReadonlyRecordClassMessage(string someText) => SomeText = someText;
        public string SomeText { get; set; }
    }
}