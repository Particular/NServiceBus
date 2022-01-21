namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_using_versioned_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Idk()
        {
            // this works:
            var t = Type.GetType("NServiceBus.AcceptanceTests.Core.Pipeline.When_using_versioned_messages+VersionedMessage, NServiceBus.AcceptanceTests, Version=42.0.0.0, Culture=neutral, PublicKeyToken=null");
            await Scenario.Define<Context>()
                .WithEndpoint<Sender>(e => e
                    .When(s => s.Send(new VersionedMessage())))
                .WithEndpoint<Receiver>()
                .Done(c => c.MessageReceived)
                .Run(TimeSpan.FromSeconds(10));
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c => c.ConfigureRouting().RouteToEndpoint(typeof(VersionedMessage), typeof(Receiver)));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register(new ContractVersionBehavior(), "changes incoming message contract version"));
            }

            public class ContractVersionBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    var enclosedMessageTypes = context.Message.Headers[Headers.EnclosedMessageTypes];
                    context.Message.Headers[Headers.EnclosedMessageTypes] = enclosedMessageTypes.Replace("8.0.0", "42.0.0");
                    return next();
                }
            }

            public class MessageHandler : IHandleMessages<VersionedMessage>
            {
                Context testContext;

                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(VersionedMessage message, IMessageHandlerContext context)
                {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                    var versionHeader = context.MessageHeaders[Headers.EnclosedMessageTypes];
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                    testContext.MessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class VersionedMessage : IMessage
        {
        }
    }
}