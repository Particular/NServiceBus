namespace NServiceBus.AcceptanceTests.Config
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NUnit.Framework;

    public class When_overriding_public_return_address : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_allow_endpoint_name_to_be_used()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithCustomAddress>(b=>b.CustomConfig(c=>c.UseEndpointNameAsPublicReturnAddress()))
                    .Done(c => c.IsDone)
                    .Run();

            Assert.AreEqual(context.ReplyToAddress.Queue,context.EndpointName);
        }

        [Test]
        public void Should_use_explicit_address_if_found()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithCustomAddress>(b =>
                    {
                        b.CustomConfig(c => c.OverridePublicReturnAddress(Address.Parse("Explicit")));
                        b.CustomConfig(c => c.UseEndpointNameAsPublicReturnAddress());
                    })
                    .Done(c => c.IsDone)
                    .Run();

            Assert.AreEqual(context.ReplyToAddress.Queue, "Explicit");
        }

        public class Context : ScenarioContext
        {
            public bool IsDone { get; set; }
            public Address ReplyToAddress { get; set; }
            public string EndpointName { get; set; }
        }

        public class EndpointWithCustomAddress : EndpointConfigurationBuilder
        {
            public EndpointWithCustomAddress()
            {
                EndpointSetup<DefaultServer>();
            }

            class AfterConfigIsComplete:IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public ReadOnlySettings Settings { get; set; }


                public void Start()
                {
                    Context.ReplyToAddress = Settings.Get<Address>("PublicReturnAddress");

                    Context.EndpointName = Settings.EndpointName();

                    Context.IsDone = true;
                }

                public void Stop()
                {
                }
            }
        }
    }


}