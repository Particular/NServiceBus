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
        [Test]
        public async Task Should_be_able_to_read_instance_specific_queue_name_using_extension_method()
        {
            var instanceDiscriminator = "instance-42";

            var context = await Scenario.Define<Context>()
                .WithEndpoint<UniquelyAddressableEndpoint>(e=>e.CustomConfig(c=>c.MakeInstanceUniquelyAddressable(instanceDiscriminator)))
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
                EndpointSetup<DefaultServer>();
            }

            public class SpyFeature : Feature
            {
                public SpyFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Settings.Get<Context>().InstanceDescriminatorFromSettingsExtensions = context.Settings.InstanceSpecificQueue();
                }
            }
        }
    }
}