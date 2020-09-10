namespace NServiceBus.AcceptanceTests.Core.Conventions
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    public class When_using_a_greedy_convention : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage
                {
                    Id = c.Id
                })))
                .Done(c => c.WasCalled)
                .Run();

            Assert.True(context.WasCalled, "The message handler should be called");
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

        public class MyMessage
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                if (testContext.Id != message.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.WasCalled = true;
                return Task.FromResult(0);
            }

            Context testContext;
        }
    }
}