﻿// This test can be removed when the InstanceSpecificQueue() extension is being fully obsoleted
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    class When_using_discriminator : NServiceBusAcceptanceTest
    {
        const string instanceDiscriminator = "instance-42";

        [Test]
        public async Task Should_be_able_to_read_instance_specific_queue_name_using_extension_method()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<UniquelyAddressableEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            StringAssert.EndsWith(instanceDiscriminator, context.InstanceDescriminatorFromSettingsExtensions);
        }

        class Context : ScenarioContext
        {
            public string InstanceDescriminatorFromSettingsExtensions { get; set; }
        }

        class UniquelyAddressableEndpoint : EndpointConfigurationBuilder
        {
            public UniquelyAddressableEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.MakeInstanceUniquelyAddressable(instanceDiscriminator));
            }

            public class SpyFeature : Feature
            {
                public SpyFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Settings.Get<Context>().InstanceDescriminatorFromSettingsExtensions = context.InstanceSpecificQueueAddress().Discriminator;
                }
            }
        }
    }
}
#pragma warning restore CS0618
#pragma warning restore IDE0079 // Remove unnecessary suppression