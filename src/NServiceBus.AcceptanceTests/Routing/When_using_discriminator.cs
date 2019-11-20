namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    class When_using_discriminator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_read_instance_specific_queue_name_using_extension_method()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(e => e
                    .When(s =>
                    {
                        var options = new SendOptions();
                        options.RouteReplyToThisInstance();
                        return s.Send(new RequestReplyMessage(), options);
                    })
                    .CustomConfig(ec => ec.EnableFeature<Sender.SpyFeature>()))
                .WithEndpoint<Replier>()
                .Done(c => c.ReplyReceived)
                .Run();

            Assert.IsTrue(context.ReplyReceived);
            StringAssert.EndsWith(instanceDiscriminator, context.ReplyToAddress);
        }

        const string instanceDiscriminator = "instance-42";

        class Context : ScenarioContext
        {
            public string ReplyToAddress { get; set; }
            public bool ReplyReceived { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.MakeInstanceUniquelyAddressable(instanceDiscriminator);
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(RequestReplyMessage), typeof(Replier));
                });
            }

            class ReplyMessageHandler : IHandleMessages<ReplyMessage>
            {
                public ReplyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    testContext.ReplyReceived = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class SpyFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    var endpointName = Conventions.EndpointNamingConvention(typeof(Sender));
                    var endpointNameWithDiscriminator = $"{endpointName}-{instanceDiscriminator}";
                    var instanceSpecificQueue = NServiceBus.SettingsExtensions.InstanceSpecificQueue(context.Settings);
                    StringAssert.AreEqualIgnoringCase(endpointNameWithDiscriminator, instanceSpecificQueue, "Instance specific discriminator was not found in the queue name.");
                }
            }
        }

        class Replier : EndpointConfigurationBuilder
        {
            public Replier()
            {
                EndpointSetup<DefaultServer>();
            }

            class RequestReplyMessageHandler : IHandleMessages<RequestReplyMessage>
            {
                public RequestReplyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(RequestReplyMessage message, IMessageHandlerContext context)
                {
                    testContext.ReplyToAddress = context.ReplyToAddress;
                    return context.Reply(new ReplyMessage());
                }

                Context testContext;
            }
        }

        public class RequestReplyMessage : ICommand
        {
        }

        public class ReplyMessage : IMessage
        {
        }
    }
}