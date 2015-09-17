namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.MessageMutator;

    public class When_performing_slr : NServiceBusAcceptanceTest
    {
        public class Context : ScenarioContext
        {
            public byte OriginalBodyChecksum { get; set; }

            public byte SlrChecksum { get; set; }

            public bool SimulateSerializationException { get; set; }

            public bool ForwardedToErrorQueue { get; set; }

            public string PhysicalMessageId { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.EnableFeature<SecondLevelRetries>();
                    configure.EnableFeature<TimeoutManager>();
                    configure.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
                })
                .WithConfig<SecondLevelRetriesConfig>(c => c.TimeIncrease = TimeSpan.FromSeconds(1));
            }

            public static byte Checksum(byte[] data)
            {
                var longSum = data.Sum(x => (long) x);
                return unchecked((byte) longSum);
            }

            class BodyMutator : IMutateOutgoingTransportMessages, IMutateIncomingTransportMessages
            {
                public Context Context { get; set; }

                public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
                {
                    var originalBody = transportMessage.Body;

                    Context.OriginalBodyChecksum = Checksum(originalBody);

                    var decryptedBody = new byte[originalBody.Length];

                    Buffer.BlockCopy(originalBody, 0, decryptedBody, 0, originalBody.Length);

                    //decrypt
                    decryptedBody[0]++;

                    if (Context.SimulateSerializationException)
                    {
                        decryptedBody[1]++;
                    }

                    transportMessage.Body = decryptedBody;
                    return Task.FromResult(0);
                }

                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    context.OutgoingBody[0]--;
                    return Task.FromResult(0);
                }
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }
                public BusNotifications BusNotifications { get; set; }

                public Task StartAsync()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.ForwardedToErrorQueue = true;
                        Context.SlrChecksum = Checksum(e.Body);
                    });
                    return Task.FromResult(0);
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public Task Handle(MessageToBeRetried message)
                {
                    Context.PhysicalMessageId = Bus.CurrentMessageContext.Id;
                    throw new SimulatedException();
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
        }
    }
}