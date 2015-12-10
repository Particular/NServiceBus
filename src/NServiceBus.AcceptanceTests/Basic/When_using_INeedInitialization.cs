namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_INeedInitialization : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_set_endpoint_name()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Sender>()
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.True(context.WasCalled, "The message handler should be called");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<SendMessage>(typeof(Sender));
            }

            public class SetEndpointName : INeedInitialization
            {
                public void Customize(BusConfiguration config)
                {
                    config.EndpointName("INeedInitialization_receiver");
                }
            }

            public class SendMessageToSender : IWantToRunWhenBusStartsAndStops
            {
                public Task Start(IBusSession session)
                {
                    return session.Send(new SendMessage());
                }

                public Task Stop(IBusSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class SendMessage : ICommand
        {
        }

        [Serializable]
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }

        public class SendMessageHandler : IHandleMessages<SendMessage>
        {
            public Task Handle(SendMessage message, IMessageHandlerContext context)
            {
                return context.Send("INeedInitialization_receiver", new MyMessage());
            }
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
