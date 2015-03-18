namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using System.Linq;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NServiceBus.Config;
    using Unicast.Messages;

    public class When_performing_slr : NServiceBusAcceptanceTest
    {
        public class Context : ScenarioContext
        {
            public bool FaultManagerInvoked { get; set; }

            public byte OriginalBodyChecksum { get; set; }

            public byte SlrChecksum { get; set; }

            public bool SimulateSerializationException { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }
   
            class BodyMutator : IMutateOutgoingPhysicalContext,IMutateIncomingTransportMessages, INeedInitialization
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

                    if (Context.SimulateSerializationException)
                        decryptedBody[1]++;

                    transportMessage.Body = decryptedBody;
                }


                public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
                {
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
                }

                public void MutateOutgoing(OutgoingPhysicalMutatorContext context)
                {
                    context.Body[0]--;
                }
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.FaultManagerInvoked = true;
                        Context.SlrChecksum = Checksum(e.Body);
                    });
                }

                public void Stop() { }
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public void Handle(MessageToBeRetried message)
                {
                    throw new Exception("Simulated exception");
                }
            }

            public static byte Checksum(byte[] data)
            {
                var longSum = data.Sum(x => (long)x);
                return unchecked((byte)longSum);
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
        }
    }
}
