namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using System.Linq;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NServiceBus.Config;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;
    using Unicast.Messages;

    public class When_sending_to_slr : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_preserve_the_original_body_for_regular_exceptions()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                    .AllowExceptions()
                    .Done(c => c.SlrChecksum != default(byte))
                    .Run();

            Assert.AreEqual(context.OriginalBodyChecksum, context.SlrChecksum, "The body of the message sent to slr should be the same as the original message coming off the queue");

        }
        
        [Test]
        public void Should_raise_FinishedMessageProcessing_event()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                    .Done(c => c.FinishedMessageProcessingCalledAfterFaultManagerInvoked)
                    .Run();

            Assert.IsTrue(context.FinishedMessageProcessingCalledAfterFaultManagerInvoked);

        }

        [Test]
        public void Should_preserve_the_original_body_for_serialization_exceptions()
        {
            var context = new Context
                {
                    SimulateSerializationException = true
                };

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                    .AllowExceptions()
                    .Done(c => c.SlrChecksum != default(byte))
                    .Run();

            Assert.AreEqual(context.OriginalBodyChecksum, context.SlrChecksum, "The body of the message sent to slr should be the same as the original message coming off the queue");

        }

        public class Context : ScenarioContext
        {
            public bool FinishedMessageProcessingCalledAfterFaultManagerInvoked { get; set; }
            public bool FaultManagerInvoked { get; set; }

            public byte OriginalBodyChecksum { get; set; }

            public byte SlrChecksum { get; set; }

            public bool SimulateSerializationException { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b => b.RegisterComponents(r => r.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance)))
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class FinishedProcessingListener : IWantToRunWhenBusStartsAndStops
            {
                readonly Context context;

                public FinishedProcessingListener(UnicastBus bus, Context context)
                {
                    this.context = context;
                    bus.Transport.FinishedMessageProcessing += Transport_FinishedMessageProcessing;
                }

                void Transport_FinishedMessageProcessing(object sender, FinishedMessageProcessingEventArgs e)
                {
                    if (context.FaultManagerInvoked)
                    {
                        context.FinishedMessageProcessingCalledAfterFaultManagerInvoked = true;
                    }
                }

                public void Start()
                {
                }

                public void Stop()
                {
                }
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

                    if (Context.SimulateSerializationException)
                        decryptedBody[1]++;

                    transportMessage.Body = decryptedBody;
                }


                public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
                {
                    transportMessage.Body[0]--;
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
                }
            }

            class CustomFaultManager : IManageMessageFailures
            {
                public Context Context { get; set; }

                public void SerializationFailedForMessage(TransportMessage message, Exception e)
                {
                    Context.FaultManagerInvoked = true;
                    Context.SlrChecksum = Checksum(message.Body);
                }

                public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
                {
                    Context.FaultManagerInvoked = true;
                    Context.SlrChecksum = Checksum(message.Body);
                }

                public void Init(Address address)
                {

                }
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
