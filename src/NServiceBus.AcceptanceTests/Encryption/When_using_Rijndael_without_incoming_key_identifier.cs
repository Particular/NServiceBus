namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_Rijndael_without_incoming_key_identifier : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_process_decrypted_message_without_key_identifier()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((bus, context) => bus.Send(new MessageWithSecretData
                {
                    Secret = "betcha can't guess my secret"
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
                    {"new", Encoding.ASCII.GetBytes("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")}
                };

                var expiredKeys = new[]
                {
                    Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
                };
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("new", keys, expiredKeys));
            }

            public class Handler : IHandleMessages<MessageWithSecretData>
            {
                public Context Context { get; set; }

                public Task Handle(MessageWithSecretData message, IMessageHandlerContext context)
                {
                    Context.Secret = message.Secret.Value;
                    Context.Done = true;
                    return Task.FromResult(0);
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
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                context.Headers.Remove(Headers.RijndaelKeyIdentifier);
                return Task.FromResult(0);
            }

            public void Customize(EndpointConfiguration configuration)
            {
                configuration.RegisterComponents(c => c.ConfigureComponent<RemoveKeyIdentifierHeaderMutator>(DependencyLifecycle.InstancePerCall));
            }
        }
    }
}