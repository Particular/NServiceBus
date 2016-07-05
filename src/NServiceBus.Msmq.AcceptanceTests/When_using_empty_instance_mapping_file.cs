namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;
    using Settings;

    public class When_using_empty_instance_mapping_file : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_messages_to_logical_endpoint_address()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderWithEmptyMappingFile>(e => e.When(async c =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        await c.Send(new Message());
                    }
                }))
                .WithEndpoint<ScaledOutReceiver>(e => e.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<ScaledOutReceiver>(e => e.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .Done(c => c.MessagesForInstance1 + c.MessagesForInstance2 >= 10)
                .Run();

            // it should send messages to the shared queue
            Assert.That(context.MessagesForInstance1, Is.GreaterThanOrEqualTo(1));
            Assert.That(context.MessagesForInstance2, Is.GreaterThanOrEqualTo(1));
        }

        public class Context : ScenarioContext
        {
            public int MessagesForInstance1;
            public int MessagesForInstance2;
        }

        public class SenderWithEmptyMappingFile : EndpointConfigurationBuilder
        {
            public SenderWithEmptyMappingFile()
            {
                var logicalEndpointName = Conventions.EndpointNamingConvention(typeof(ScaledOutReceiver));

                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "instance-mapping.xml");

                // only configure instance 2 for the routing to make sure messages aren't sent to the shared queue
                File.WriteAllText(filePath,
$@"<endpoints>
    <endpoint name=""{logicalEndpointName}"">
    </endpoint>
</endpoints>");

                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>().Routing()
                        .RouteToEndpoint(typeof(Message), logicalEndpointName);
                });
            }
        }

        public class ScaledOutReceiver : EndpointConfigurationBuilder
        {
            public ScaledOutReceiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageHandler : IHandleMessages<Message>
            {
                Context testContext;
                ReadOnlySettings settings;

                public MessageHandler(Context testContext, ReadOnlySettings settings)
                {
                    this.testContext = testContext;
                    this.settings = settings;
                }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    var instanceDiscriminator = settings.Get<string>("EndpointInstanceDiscriminator");
                    if (instanceDiscriminator == "1")
                    {
                        Interlocked.Increment(ref testContext.MessagesForInstance1);
                    }
                    if (instanceDiscriminator == "2")
                    {
                        Interlocked.Increment(ref testContext.MessagesForInstance2);
                    }

                    return Task.FromResult(0);
                }
            }
        }

        public class Message : ICommand
        {
        }
    }
}