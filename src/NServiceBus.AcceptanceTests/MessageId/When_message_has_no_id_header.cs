namespace NServiceBus.AcceptanceTests.MessageId;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_message_has_no_id_header : NServiceBusAcceptanceTest
{
    [Test]
    public async Task A_message_id_is_generated_by_the_transport_layer_on_receiving_side()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(g => g.When(b => b.SendLocal(new Message())))
            .Run();

        Assert.That(string.IsNullOrWhiteSpace(context.MessageId), Is.False);
    }

    class CorruptionBehavior : IBehavior<IDispatchContext, IDispatchContext>
    {
        public Task Invoke(IDispatchContext context, Func<IDispatchContext, Task> next)
        {
            context.Operations.First().Message.Headers[Headers.MessageId] = null;

            return next(context);
        }
    }

    public class Context : ScenarioContext
    {
        public string MessageId { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register("CorruptionBehavior", new CorruptionBehavior(), "Corrupting the message id"));

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<Message>
        {
            public Task Handle(Message message, IMessageHandlerContext context)
            {
                testContext.MessageId = context.MessageId;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Message : IMessage;
}
