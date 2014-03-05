namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Linq;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NUnit.Framework;

#pragma warning disable 612, 618

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_preserve_the_original_body()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.AuditChecksum != default(byte))
                    .Run();

            Assert.AreEqual(context.OriginalBodyChecksum, context.AuditChecksum, "The body of the message sent to audit should be the same as the original message coming off the queue");
        }

        [Test]
        public void Should_add_the_license_diagnostic_headers()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.HasDiagnosticLicensingHeaders)
                    .Run();
            Assert.IsTrue(context.HasDiagnosticLicensingHeaders);
        }


        public class Context : ScenarioContext
        {
            public byte OriginalBodyChecksum { get; set; }
            public byte AuditChecksum { get; set; }
            public bool HasDiagnosticLicensingHeaders { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<AuditSpyEndpoint>();
            }

            class BodyMutator : IMutateTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public void MutateIncoming(TransportMessage transportMessage)
                {

                    var originalBody = transportMessage.Body;

                    Context.OriginalBodyChecksum = Checksum(originalBody);

                    var decryptedBody = new byte[originalBody.Length];

                    Buffer.BlockCopy(originalBody,0,decryptedBody,0,originalBody.Length);
                   
                    //decrypt
                    decryptedBody[0]++;

                    transportMessage.Body = decryptedBody;
                }


                public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
                {
                    //not the way to do it for real but good enough for this test
                    transportMessage.Body[0]--;
                }

                public void Init()
                {
                    Configure.Component<BodyMutator>(DependencyLifecycle.InstancePerCall);
                }
            }


            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>{ public void Handle(MessageToBeAudited message) {}}
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class BodySpy : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public void MutateIncoming(TransportMessage transportMessage)
                {
                    Context.AuditChecksum = Checksum(transportMessage.Body);
                    string licenseExpired;
                    Context.HasDiagnosticLicensingHeaders = transportMessage.Headers.TryGetValue(Headers.HasLicenseExpired, out licenseExpired);
                }

                public void Init()
                {
                    Configure.Component<BodySpy>(DependencyLifecycle.InstancePerCall);
                }
            }
        }

        public static byte Checksum(byte[] data)
        {
            var longSum = data.Sum(x => (long)x);
            return unchecked((byte)longSum);
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }
    }

#pragma warning restore  612, 618

}