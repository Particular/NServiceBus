namespace NServiceBus.IntegrationTests.Automated.BasicMessaging
{
    using System;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Support;

    [TestFixture]
    public class When_sending_a_message_to_another_endpoint : NServiceBusIntegrationTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            Scenario.Define()
                .WithEndpoint<Sender>()
                .WithEndpoint<Receiver>(() => new ReceiveContext())
                .Repeat(r => 
                         r.For<AllTransports>()
                         .For<AllSerializers>()
                         //.For<AllBuilders>()
                         )
                .Should<ReceiveContext>(c =>
                    {
                        Assert.True(c.WasCalled,"Message handler was not called as expected");
                        Assert.AreEqual(1, c.TimesCalled, "Message handler should only be invoked once");
                    })
                .Run();
        }


        public class ReceiveContext : BehaviorContext
        {
            public bool WasCalled { get; set; }

            public int TimesCalled { get; set; }
        }

        public class Sender : EndpointBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(Receiver))
                    .When(bus =>bus.Send(new MyMessage()));
            }
        }

        public class Receiver : EndpointBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>()
                    .Done((ReceiveContext context) => context.WasCalled);
            }

        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            private readonly ReceiveContext context;

            public MyMessageHandler(ReceiveContext context)
            {
                this.context = context;
            }

            public void Handle(MyMessage message)
            {
                this.context.WasCalled = true;
                context.TimesCalled++;
            }
        }
    }
}
