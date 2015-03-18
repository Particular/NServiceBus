﻿namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_to_another_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given((bus, c) =>
                    {
                        var sendContext = new SendContext();

                        sendContext.SetHeader("MyHeader", "MyHeaderValue");
                        bus.Send(new MyMessage{Id = c.Id},sendContext);
                    }))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.True(context.WasCalled, "The message handler should be called");
            Assert.AreEqual(1, context.TimesCalled, "The message handler should only be invoked once");
            Assert.AreEqual("StaticHeaderValue", context.ReceivedHeaders["MyStaticHeader"], "Static headers should be attached to outgoing messages");
            Assert.AreEqual("MyHeaderValue", context.MyHeader, "Static headers should be attached to outgoing messages");
                       
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
                EndpointSetup<DefaultServer>(c => c.OutgoingHeaders.Add("MyStaticHeader", "StaticHeaderValue"))
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
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

                    Context.MyHeader = Bus.CurrentMessageContext.Headers["MyHeader"];

                    Context.ReceivedHeaders = Bus.CurrentMessageContext.Headers;

                    Context.WasCalled = true;
                }
            }
        }

        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}
