namespace NServiceBus.AcceptanceTests.Routing;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus.Routing;
using NUnit.Framework;

public class When_making_endpoint_uniquely_addressable : NServiceBusAcceptanceTest
{
    static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));
    const string InstanceDiscriminator = "XYZ";

    [Test]
    public async Task Should_be_addressable_both_by_shared_queue_and_unique_queue()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Receiver>()
            .WithEndpoint<UnawareOfInstanceSender>(b => b.When(s => s.Send(new MyMessage())))
            .WithEndpoint<InstanceAwareSender>(b => b.When(s => s.Send(new MyMessage())))
            .Run();

        Assert.That(context.MessagesReceived, Is.EqualTo(2));
    }

    public class Context : ScenarioContext
    {
        public int MessagesReceived;
    }

    public class UnawareOfInstanceSender : EndpointConfigurationBuilder
    {
        public UnawareOfInstanceSender() =>
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), ReceiverEndpoint);
            });
    }

    public class InstanceAwareSender : EndpointConfigurationBuilder
    {
        public InstanceAwareSender() =>
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), ReceiverEndpoint);
                c.GetSettings().GetOrCreate<EndpointInstances>()
                    .AddOrReplaceInstances("testing",
                    [
                        new EndpointInstance(ReceiverEndpoint, InstanceDiscriminator)
                    ]);
            });
    }

    public class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() => EndpointSetup<DefaultServer>(c => { c.MakeInstanceUniquelyAddressable(InstanceDiscriminator); });

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.MessagesReceived);
                testContext.MarkAsCompleted(count > 1);
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage;
}