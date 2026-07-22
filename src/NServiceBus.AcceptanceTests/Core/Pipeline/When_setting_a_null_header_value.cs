namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_setting_a_null_header_value : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_deliver_message_with_null_header_value()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session =>
            {
                var options = new SendOptions();
                options.SetHeader("MyHeader", null);
                options.RouteToThisEndpoint();

                return session.Send(new MyMessage(), options);
            }))
            .Done(c => c.WasCalled)
            .Run();
    }

    public class Context : ScenarioContext
    {
        public bool WasCalled { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class Handler : IHandleMessages<MyMessage>
        {
            Context testContext;
            public Handler(Context testContext) => this.testContext = testContext;

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.WasCalled = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage { }
}