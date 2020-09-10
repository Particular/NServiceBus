namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_extending_sendoptions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_set_context_items_and_retrieve_it_via_a_behavior()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SendOptionsExtensions>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.GetExtensions().Set(new SendOptionsExtensions.TestingSendOptionsExtensionBehavior.Context
                    {
                        SomeValue = "I did it"
                    });
                    options.RouteToThisEndpoint();

                    return session.Send(new SendMessage(), options);
                }))
                .Done(c => c.WasCalled)
                .Run();

            Assert.AreEqual("I did it", context.Secret);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public string Secret { get; set; }
        }

        public class SendOptionsExtensions : EndpointConfigurationBuilder
        {
            public SendOptionsExtensions()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("TestingSendOptionsExtension", new TestingSendOptionsExtensionBehavior(), "Testing send options extensions"));
            }

            class SendMessageHandler : IHandleMessages<SendMessage>
            {
                public SendMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SendMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.Secret = message.Secret;
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class TestingSendOptionsExtensionBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
            {
                public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
                {
                    if (context.Extensions.TryGet(out Context data))
                    {
                        context.UpdateMessage(new SendMessage
                        {
                            Secret = data.SomeValue
                        });
                    }

                    return next(context, cancellationToken);
                }

                public class Context
                {
                    public string SomeValue { get; set; }
                }
            }
        }

        public class SendMessage : ICommand
        {
            public string Secret { get; set; }
        }
    }
}