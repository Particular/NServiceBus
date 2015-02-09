namespace NServiceBus.AcceptanceTests.Volatile
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_sending_to_non_durable_endpoint: NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            var context = new Context();
            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given((bus, c) => bus.Send(new MyMessage())))
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
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.DisableDurableMessages();
                    builder.DiscardFailedMessagesInsteadOfSendingToErrorQueue(); // to avoid creating the error q, it might blow up for brokers (RabbitMQ)
                })
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.DisableDurableMessages();
                    builder.DiscardFailedMessagesInsteadOfSendingToErrorQueue(); // to avoid creating the error q, it might blow up for brokers (RabbitMQ)
                });
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public void Handle(MyMessage message)
            {
                Context.WasCalled = true;
            }
        }
    }
}
