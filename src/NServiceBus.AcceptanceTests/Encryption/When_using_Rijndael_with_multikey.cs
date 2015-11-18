namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_Rijndael_with_multikey: NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_decrypted_message()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.When((bus, context) => bus.Send(new MessageWithSecretData
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
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"))
                    .AddMapping<MessageWithSecretData>(typeof(Receiver));
            }

        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                var expiredKeys = new List<string>
                {
                    "gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"
                };
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6", expiredKeys));
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


    }
}