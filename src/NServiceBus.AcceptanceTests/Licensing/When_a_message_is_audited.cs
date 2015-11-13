namespace NServiceBus.AcceptanceTests.Licensing
{
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_add_the_license_diagnostic_headers()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithAuditOn>(b => b.When(bus => bus.SendLocal(new MessageToBeAudited())))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.Done)
                    .Run();

            Assert.IsTrue(context.HasDiagnosticLicensingHeaders);

            if (Debugger.IsAttached)
            {
                Assert.True(context.Logs.Any(m => m.Level == "error" && m.Message.StartsWith("Your license has expired")), "Error should be logged");
            }
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
                EndpointSetup<DefaultServer>(c => c.License(ExpiredLicense))
                    .AuditTo<AuditSpyEndpoint>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
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
                public Context TestContext { get; set; }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    string licenseExpired;

                    TestContext.HasDiagnosticLicensingHeaders = context.MessageHeaders.TryGetValue(Headers.HasLicenseExpired, out licenseExpired);

                    TestContext.Done = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class MessageToBeAudited : IMessage
        {
        }

        static string ExpiredLicense = @"<?xml version=""1.0"" encoding=""utf-8""?>
<license id = ""b13ba7a3-5fe8-4745-a041-2d6a9f7462cf"" expiration=""2015-03-18T00:00:00.0000000"" type=""Subscription"" Applications=""All"" NumberOfNodes=""4"" UpgradeProtectionExpiration=""2015-03-18"">
  <name>Ultimate Test</name>
  <Signature xmlns = ""http://www.w3.org/2000/09/xmldsig#"">
    <SignedInfo>
      <CanonicalizationMethod Algorithm= ""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
      <SignatureMethod Algorithm= ""http://www.w3.org/2000/09/xmldsig#rsa-sha1"" />
      <Reference URI= """">
        <Transforms>
          <Transform Algorithm= ""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" />
        </Transforms>
        <DigestMethod Algorithm= ""http://www.w3.org/2000/09/xmldsig#sha1"" />
        <DigestValue>kz07xp2x3tjk+ixQglCHq40RJg8=</DigestValue>
      </Reference>
    </SignedInfo>
    <SignatureValue>WN0zCL3i2vvwtPFI7/Qbo8ymhJFeYpauFqFbuFynOfWrKd5PMfcY1ToWZyz1vs6dLFL9kPngtVRX9yZZXC1y6la8oS/rnBq0Jwm2pFqCtIVtXKee93dTTx7Bij9x7XUBtAVpZDszbZPfLnrdHwS4BFn4CTvOJRiSUEB1ks1ONiQ=</SignatureValue>
  </Signature>
</license>
";
    }
}
