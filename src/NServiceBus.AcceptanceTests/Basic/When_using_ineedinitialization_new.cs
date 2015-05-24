namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_ineedinitialization_new : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_able_to_set_endpoint_name()
        {
            var context = new Context();

            Scenario.Define(context)
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
                    config.EndpointName("ineedinitialization_receiver");
                }
            }

            public class SendMessageToSender: IRunWhenBusStartsAndStops
            {
                public void Start(IRunContext context)
                {
                    context.Send(new SendMessage());
                }

                public void Stop(IRunContext context)
                {
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

        public class SendMessageHandler : IProcessCommands<SendMessage>
        {
            public void Handle(SendMessage message, ICommandContext context)
            {
                context.Send("ineedinitialization_receiver", new MyMessage());
            }
        }

        public class MyMessageHandler : IProcessCommands<MyMessage>
        {
            public Context Context { get; set; }

            public void Handle(MyMessage message, ICommandContext context)
            {
                Context.WasCalled = true;
            }
        }
    }
}
