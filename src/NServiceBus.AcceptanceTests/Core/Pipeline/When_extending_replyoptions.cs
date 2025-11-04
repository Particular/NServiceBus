namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Transport;

public class When_extending_replyoptions : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_make_extensions_available_to_pipeline()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReplyOptionsExtensions>(b => b.When((session, _) => session.SendLocal(new SendMessage())))
            .Done(c => c.DataReceived is not null)
            .Run();

        Assert.That(context.DataReceived, Is.EqualTo("ItWorksItWorks"));
    }

    public class Context : ScenarioContext
    {
        public string DataReceived { get; set; }
    }

    public class ReplyOptionsExtensions : EndpointConfigurationBuilder
    {
        public ReplyOptionsExtensions() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register("TestingSendOptionsExtension", new TestingReplyOptionsExtensionBehavior(), "Testing send options extensions"));

        class SendMessageHandler(Context testContext) : IHandleMessages<SendMessage>
        {
            public Task Handle(SendMessage message, IMessageHandlerContext context)
            {
                var options = new ReplyOptions();

                var data = new ReplyOptionsExtensions.TestingReplyOptionsExtensionBehavior.Context
                {
                    Data = "ItWorks"
                };
                options.GetExtensions().Set(data);

                options.GetDispatchProperties().Extensions.Add("Context", data);

                return context.Reply(new ReplyMessage(), options);
            }
        }

        class ReplyMessageHandler(Context testContext) : IHandleMessages<ReplyMessage>
        {
            public Task Handle(ReplyMessage message, IMessageHandlerContext context)
            {
                testContext.DataReceived = message.Data;
                return Task.CompletedTask;
            }
        }

        public class TestingReplyOptionsExtensionBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
        {
            public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
            {
                if (context.Message.Instance is ReplyMessage)
                {
                    var newInstance = new ReplyMessage();

                    if (context.Extensions.TryGet<Context>(out var data))
                    {
                        newInstance.Data = data.Data;
                    }

                    if (context.Extensions.TryGet<DispatchProperties>(out var properties) &&
                        properties.Extensions.TryGetValue("Context", out var contextAsObject)
                        && contextAsObject is Context dispatchContext)
                    {
                        newInstance.Data += dispatchContext.Data;
                    }

                    context.UpdateMessage(newInstance);
                }

                return next(context);
            }

            public class Context
            {
                public string Data { get; set; }
            }
        }
    }

    public class SendMessage : ICommand;

    public class ReplyMessage : IMessage
    {
        public string Data { get; set; }
    }
}