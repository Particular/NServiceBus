﻿namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;
    using Settings;

    public class When_using_instance_mapping_file : NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetupMappingFile()
        {
            // this can't be static because the conventions are setup in the NServiceBusAcceptanceTest base class
            destination = Conventions.NameOf<ScaledOutReceiver>();

            // e.g. spelling error in endpoint:
            File.WriteAllText(mappingFilePath,
                $@"<endpoints>
    <endpoint name=""{destination}"">
        <instance discriminator=""2""/>
    </endpoint>
</endpoints>");
        }

        [TearDown]
        public void DeleteMappingFile()
        {
            File.Delete(mappingFilePath);
        }

        [Test]
        public async Task Should_send_messages_to_configured_instances()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderWithMappingFile>(e => e.When(async c =>
                {
                    for (var i = 0; i < 5; i++)
                    {
                        await c.Send(new Message());
                    }
                }))
                .WithEndpoint<ScaledOutReceiver>(e => e.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<ScaledOutReceiver>(e => e.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .Done(c => c.MessagesForInstance1 + c.MessagesForInstance2 >= 5)
                .Run();

            Assert.That(context.MessagesForInstance1, Is.EqualTo(0));
            Assert.That(context.MessagesForInstance2, Is.EqualTo(5));
        }

        static string mappingFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nameof(When_starting_with_invalid_instance_mapping_file) + ".xml");
        static string destination;

        public class Context : ScenarioContext
        {
            public int MessagesForInstance1;
            public int MessagesForInstance2;
        }

        public class SenderWithMappingFile : EndpointConfigurationBuilder
        {
            public SenderWithMappingFile()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var routingSettings = c.UseTransport<MsmqTransport>().Routing();
                    routingSettings.RouteToEndpoint(typeof(Message), destination);
                    routingSettings.InstanceMappingFile().FilePath(mappingFilePath);
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

                Context testContext;
                ReadOnlySettings settings;
            }
        }

        public class Message : ICommand
        {
        }
    }
}