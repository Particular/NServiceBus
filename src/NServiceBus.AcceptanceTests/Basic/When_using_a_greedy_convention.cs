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
                    .WithEndpoint<EndPoint>(b => b.Given((bus, c) =>
                    {
                        bus.SendLocal(new MyMessage { Id = c.Id });
                        return Task.FromResult(0);
                    }))
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

        public class EndPoint : EndpointConfigurationBuilder
        {
            public EndPoint()
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
            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.WasCalled = true;
            }
        }
    }
}