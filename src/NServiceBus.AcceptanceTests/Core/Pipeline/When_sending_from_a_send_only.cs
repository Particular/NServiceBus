namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_from_a_send_only : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_send_the_message()
    {
        var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
            .WithEndpoint<Sender>(b => b.When((session, c) => session.Send(new MyMessage
            {
                Id = c.Id
            })))
            .WithEndpoint<Receiver>()
            .Run();

        Assert.That(context.WasCalled, Is.True, "The message handler should be called");
    }

    public class Context : ScenarioContext
    {
        public bool WasCalled { get; set; }
        public Guid Id { get; set; }
    }

    public class SendOnlyEndpoint : EndpointConfigurationBuilder
    {
        public SendOnlyEndpoint() => EndpointSetup<DefaultServer>(c => c.SendOnly());
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.SendOnly();
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(Receiver));
            });
    }

    public class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() => EndpointSetup<DefaultServer>();
    }

    public class MyMessage : ICommand
    {
        public Guid Id { get; set; }
    }

    [Handler]
    public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            if (testContext.Id != message.Id)
            {
                return Task.CompletedTask;
            }

            testContext.WasCalled = true;
            testContext.MarkAsCompleted();
            return Task.CompletedTask;
        }
    }
}