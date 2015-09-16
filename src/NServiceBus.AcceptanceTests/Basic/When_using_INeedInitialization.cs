﻿namespace NServiceBus.AcceptanceTests.Basic
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
                    config.EndpointName("ineedinitialization_receiver");
                }
            }

            public class SendMessageToSender : IWantToRunWhenBusStartsAndStops
            {
                public IBus Bus { get; set; }

                public void Start()
                {
                    Bus.Send(new SendMessage());
                }

                public Task StopAsync()
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
            public IBus Bus { get; set; }

            public Task Handle(SendMessage message)
            {
                Bus.Send("ineedinitialization_receiver", new MyMessage());
                return Task.FromResult(0);
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public Task Handle(MyMessage message)
            {
                Context.WasCalled = true;
                return Task.FromResult(0);
            }
        }
    }
}
