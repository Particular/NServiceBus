namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using System.Collections.Generic;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_a_message_to_another_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<Sender>(b => b.Given((bus, context) => bus.Send(new MyMessage { Id = context.Id })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Repeat(r =>
                            r.For<AllTransports>()
                               .For<AllSerializers>()
                                  .For<AllBuilders>()
                    )
                    .Should(c =>
                        {
                            Assert.True(c.WasCalled, "The message handler should be called");
                            Assert.AreEqual(1, c.TimesCalled, "The message handler should only be invoked once");
                            Assert.AreEqual(Environment.MachineName, c.ReceivedHeaders[Headers.OriginatingMachine], "The sender should attach the machine name as a header");
                            Assert.True(c.ReceivedHeaders[Headers.OriginatingEndpoint].Contains("Sender"), "The sender should attach its endpoint name as a header");
                            Assert.AreEqual(Environment.MachineName, c.ReceivedHeaders[Headers.ProcessingMachine], "The receiver should attach the machine name as a header");
                            Assert.True(c.ReceivedHeaders[Headers.ProcessingEndpoint].Contains("Receiver"), "The receiver should attach its endpoint name as a header");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }

            public int TimesCalled { get; set; }

            public IDictionary<string, string> ReceivedHeaders { get; set; }

            public Guid Id { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.TimesCalled++;

                Context.ReceivedHeaders = Bus.CurrentMessageContext.Headers;

                Context.WasCalled = true;
            }
        }
    }
}
