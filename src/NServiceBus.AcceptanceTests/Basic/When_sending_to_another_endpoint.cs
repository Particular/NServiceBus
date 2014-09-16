namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_sending_to_another_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<Sender>(b => b.Given((bus, context) =>
                    {
                        bus.OutgoingHeaders["MyStaticHeader"] = "StaticHeaderValue";
                        bus.Send<MyMessage>(m=>
                        {
                            m.Id = context.Id;
                            bus.SetMessageHeader(m, "MyHeader", "MyHeaderValue");
                        });
                    }))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Repeat(r =>r.For(Serializers.Binary))
                    .Should(c =>
                        {
                            Assert.True(c.WasCalled, "The message handler should be called");
                            Assert.AreEqual(1, c.TimesCalled, "The message handler should only be invoked once");
                            Assert.AreEqual("StaticHeaderValue",c.ReceivedHeaders["MyStaticHeader"], "Static headers should be attached to outgoing messages");
                            Assert.AreEqual("MyHeaderValue", c.MyHeader, "Static headers should be attached to outgoing messages");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }

            public int TimesCalled { get; set; }

            public IDictionary<string, string> ReceivedHeaders { get; set; }

            public Guid Id { get; set; }

            public string MyHeader { get; set; }
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

                Context.MyHeader = Bus.GetMessageHeader(message, "MyHeader");

                Context.ReceivedHeaders = Bus.CurrentMessageContext.Headers;

                Context.WasCalled = true;
            }
        }
    }
}
