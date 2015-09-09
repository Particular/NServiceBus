namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_Rijndael_without_incoming_key_identifier : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_process_decrypted_message_without_key_identifier()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given((bus, context) => bus.Send(new MessageWithSecretData
                        {
                            Secret = "betcha can't guess my secret",
                        })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.Done)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.AreEqual("betcha can't guess my secret", c.Secret))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string Secret { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("will-be-removed-by-transport-mutator", Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")))
                    .AddMapping<MessageWithSecretData>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                var keys = new Dictionary<string, byte[]>
                {
                    {"new", Encoding.ASCII.GetBytes("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb") },
                };

                var expiredKeys = new[] { Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa") };
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("new", keys, expiredKeys));

            }

            public class Handler : IHandleMessages<MessageWithSecretData>
            {
                public Context Context { get; set; }

                public void Handle(MessageWithSecretData message)
                {
                    Context.Secret = message.Secret.Value;
                    Context.Done = true;
                }
            }
        }

        [Serializable]
        public class MessageWithSecretData : IMessage
        {
            public WireEncryptedString Secret { get; set; }
        }


        class RemoveKeyIdentifierHeaderMutator : IMutateIncomingTransportMessages, INeedInitialization
        {
            public void MutateIncoming(TransportMessage transportMessage)
            {
                transportMessage.Headers.Remove(Headers.RijndaelKeyIdentifier);
            }

            public void Customize(BusConfiguration configuration)
            {
                configuration.RegisterComponents(c => c.ConfigureComponent<RemoveKeyIdentifierHeaderMutator>(DependencyLifecycle.InstancePerCall));
            }
        }
    }
}
