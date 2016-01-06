namespace NServiceBus.AcceptanceTests.Volatile
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_sending_to_non_durable_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.When((bus, c) => bus.Send(new MyMessage())))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(builder => builder.DisableDurableMessages())
                    .AddMapping<MyMessage>(typeof(Receiver))
                    .WithConfig<MessageForwardingInCaseOfFaultConfig>(c =>
                    {
                        c.ErrorQueue = "NonDurableError";
                    });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(builder => builder.DisableDurableMessages())
                    .WithConfig<MessageForwardingInCaseOfFaultConfig>(c =>
                    {
                        c.ErrorQueue = "NonDurableError";
                    });
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                Context.WasCalled = true;
                return Task.FromResult(0);
            }
        }
    }
}