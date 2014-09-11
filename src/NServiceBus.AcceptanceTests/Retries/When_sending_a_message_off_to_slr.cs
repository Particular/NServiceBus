namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using System.Linq;
    using Config;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NUnit.Framework;
    using Unicast.Messages;

    public class When_sending_a_message_off_to_slr : NServiceBusAcceptanceTest
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
                    Context.SlrChecksum = Checksum(message.Body);
                }

                public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
                {
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