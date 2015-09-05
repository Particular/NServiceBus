namespace NServiceBus.AcceptanceTests.Licensing
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_add_the_license_diagnostic_headers()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus =>
                    {
                        bus.SendLocal(new MessageToBeAudited());
                        return Task.FromResult(0);
                    }))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.HasDiagnosticLicensingHeaders)
                    .Run();

            Assert.IsTrue(context.HasDiagnosticLicensingHeaders);
        }


        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool HasDiagnosticLicensingHeaders { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<AuditSpyEndpoint>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public void Handle(MessageToBeAudited message)
                {
                }
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }


            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    string licenseExpired;

                    Context.HasDiagnosticLicensingHeaders = Bus.CurrentMessageContext.Headers.TryGetValue(Headers.HasLicenseExpired, out licenseExpired);

                    Context.Done = true;
                }
            }
        }

        public class MessageToBeAudited : IMessage
        {
        }
    }
}
