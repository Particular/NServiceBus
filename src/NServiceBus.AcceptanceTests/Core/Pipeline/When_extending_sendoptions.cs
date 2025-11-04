namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Transport;

public class When_extending_sendoptions : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_make_extensions_available_to_pipeline()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendOptionsExtensions>(b => b.When((session, c) =>
            {
                var options = new SendOptions();

                var context = new SendOptionsExtensions.TestingSendOptionsExtensionBehavior.Context
                {
                    Data = "ItWorks"
                };
                options.GetExtensions().Set(context);
                options.RouteToThisEndpoint();

                options.GetDispatchProperties().Extensions.Add("Context", context);

                return session.Send(new SendMessage(), options);
            }))
            .Done(c => c.DataReceived is not null)
            .Run();

        Assert.That(context.DataReceived, Is.EqualTo("ItWorksItWorks"));
    }

    public class Context : ScenarioContext
    {
        public string DataReceived { get; set; }
    }

    public class SendOptionsExtensions : EndpointConfigurationBuilder
    {
        public SendOptionsExtensions() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register("TestingSendOptionsExtension", new TestingSendOptionsExtensionBehavior(), "Testing send options extensions"));

        class SendMessageHandler(Context testContext) : IHandleMessages<SendMessage>
        {
            public Task Handle(SendMessage message, IMessageHandlerContext context)
            {
                testContext.DataReceived = message.Data;
                return Task.CompletedTask;
            }
        }

        public class TestingSendOptionsExtensionBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
        {
            public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
            {
                var newInstance = new SendMessage();

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

                return next(context);
            }

            public class Context
            {
                public string Data { get; set; }
            }
        }
    }

    public class SendMessage : ICommand
    {
        public string Data { get; set; }
    }
}