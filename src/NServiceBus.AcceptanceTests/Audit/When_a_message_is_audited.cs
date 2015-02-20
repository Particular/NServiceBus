namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Linq;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        Context context;

        [SetUp]
        public new void SetUp()
        {
            context = new Context
            {
                RunId = Guid.NewGuid()
            };

            Scenario.Define(context)
                    .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited(){RunId = context.RunId})))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.Done)
                    .Run();
        }

        [Test]
        public void Should_preserve_the_original_body()
        {
            Assert.AreEqual(context.OriginalBodyChecksum, context.AuditChecksum, "The body of the message sent to audit should be the same as the original message coming off the queue");
        }

        [Test]
        public void Should_be_stamped_with_host_id_and_host_name()
        {
            Assert.IsNotNull(context.HostId);
            Assert.IsNotNull(context.HostName);
        }

        [Test, Ignore("To be fixed by andreas")]
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
            public Guid RunId { get; set; }
            public bool Done { get; set; }
            public byte OriginalBodyChecksum { get; set; }
            public byte AuditChecksum { get; set; }
            public bool HasDiagnosticLicensingHeaders { get; set; }
            public string HostId { get; set; }
            public string HostName { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<AuditSpyEndpoint>();
            }

            class BodyMutator : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public void MutateIncoming(TransportMessage transportMessage)
                {

                    var originalBody = transportMessage.Body;

                    Context.OriginalBodyChecksum = Checksum(originalBody);

                    // modifying the body by adding a line break
                    var modifiedBody = new byte[originalBody.Length + 1];

                    Buffer.BlockCopy(originalBody, 0, modifiedBody, 0, originalBody.Length);

                    modifiedBody[modifiedBody.Length - 1] = 13;

                    transportMessage.Body = modifiedBody;
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
                }
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

            class BodySpy : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public void MutateIncoming(TransportMessage transportMessage)
                {
                    Context.AuditChecksum = Checksum(transportMessage.Body);
                    string licenseExpired;
                    Context.HasDiagnosticLicensingHeaders = transportMessage.Headers.TryGetValue(Headers.HasLicenseExpired, out licenseExpired);
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<BodySpy>(DependencyLifecycle.InstancePerCall));
                }
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    if (message.RunId != Context.RunId)
                    {
                        return;
                    }
                    Context.HostId = Bus.GetMessageHeader(message, Headers.HostId);
                    Context.HostName = Bus.GetMessageHeader(message, Headers.HostDisplayName);
                    Context.Done = true;
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
            public Guid RunId { get; set; }
        }
    }
}
