﻿namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_with_conventions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Endpoint>(b => b.When(async(session, c) =>
                {
                    await session.SendLocal(new MyMessage { Id = c.Id});
                    await session.SendLocal(new MyInterfaceMessage { Id = c.Id });
                }))
                .Done(c => c.MessageClassReceived && c.MessageInterfaceReceived)
                .Run();

            Assert.True(context.MessageClassReceived);
            Assert.True(context.MessageInterfaceReceived);
        }

        public class Context : ScenarioContext
        {
            public bool MessageClassReceived { get; set; }
            public bool MessageInterfaceReceived { get; set; }
            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b => b.Conventions().DefiningMessagesAs(type => type.Name.EndsWith("Message")));
            }
        }

        public class MyMessage
        {
            public Guid Id { get; set; }
        }

        public class MyInterfaceMessage
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (Context.Id != message.Id)
                {
                    return Task.FromResult(0);
                }

                Context.MessageClassReceived = true;

                return Task.FromResult(0);
            }
        }

        public class MyMessageInterfaceHandler : IHandleMessages<MyInterfaceMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyInterfaceMessage interfaceMessage, IMessageHandlerContext context)
            {
                if (Context.Id != interfaceMessage.Id)
                {
                    return Task.FromResult(0);
                }

                Context.MessageInterfaceReceived = true;

                return Task.FromResult(0);
            }
        }
    }
}