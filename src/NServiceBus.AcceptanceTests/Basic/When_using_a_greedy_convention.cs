namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_using_a_greedy_convention : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocal(new MyMessage { Id = c.Id })))
                    .Done(c => c.WasCalled)
                    .Repeat(r => r
                        .For(Transports.Default)
                    )
                    .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }

            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Conventions().DefiningMessagesAs(MessageConvention));
            }

            static bool MessageConvention(Type t)
            {
                return t.Namespace != null &&
                       (t.Assembly == typeof(Conventions).Assembly) || (t == typeof(MyMessage));
            }
        }

        [Serializable]
        public class MyMessage
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (Context.Id != message.Id)
                    return Task.FromResult(0);

                Context.WasCalled = true;
                return Task.FromResult(0);
            }
        }
    }
}