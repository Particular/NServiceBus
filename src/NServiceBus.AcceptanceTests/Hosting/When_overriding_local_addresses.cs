namespace NServiceBus.AcceptanceTests.Hosting
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_overriding_local_addresses : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_custom_queue_names()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.IsTrue(context.Done);
            Assert.IsTrue(context.InputQueue.StartsWith("OverriddenLocalAddress"));
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>((c, d) =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.UseTransport(d.GetTransportType())
                        .Transactions(TransportTransactionMode.None)
                        .AddAddressTranslationRule(address => "OverriddenLocalAddress" + address.Qualifier); //Overriding -> Overridden
                });
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                Context.InputQueue = context.MessageHeaders[Headers.ReplyToAddress];
                Context.Done = true;
                return Task.FromResult(0);
            }
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string InputQueue { get; set; }
        }

        public class MyMessage : ICommand
        {
        }
        
    }
}