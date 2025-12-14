namespace NServiceBus.AcceptanceTests.Core.Routing;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_replying_with_pre_created_interface : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_route_to_sender()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(c => c.When(b => b.SendLocal(new MyRequest())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.GotTheReply, Is.True);
            Assert.That(context.MessageTypeInPipeline, Is.EqualTo(typeof(IMyReply)));
        }
    }

    public class Context : ScenarioContext
    {
        public bool GotTheReply { get; set; }
        public Type MessageTypeInPipeline { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>((c, r) => c.Pipeline.Register("MessageTypeSpy", new MessageTypeSpy((Context)r.ScenarioContext), "MessageTypeSpy"));

        public class StartMessageHandler(IMessageCreator messageCreator) : IHandleMessages<MyRequest>
        {
            public Task Handle(MyRequest message, IMessageHandlerContext context)
            {
                var interfaceMessage = messageCreator.CreateInstance<IMyReply>();
                return context.Reply(interfaceMessage);
            }
        }

        public class MyMessageHandler(Context testContext) : IHandleMessages<IMyReply>
        {
            public Task Handle(IMyReply message, IMessageHandlerContext context)
            {
                testContext.GotTheReply = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        class MessageTypeSpy(Context testContext) : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
        {
            public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
            {
                testContext.MessageTypeInPipeline = context.Message.MessageType;
                return next(context);
            }
        }
    }

    public class MyRequest : IMessage;

    public interface IMyReply : IMessage;
}