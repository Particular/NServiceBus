namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using Config;
    using Config.ConfigurationSource;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_encryption_with_multikey : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_decrypted_message()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given((bus, context) => bus.Send(new MessageWithSecretData
                        {
                            Secret = "betcha can't guess my secret",
                        })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.Done)
                    .Repeat(r => r.For<AllSerializers>())
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
                EndpointSetup<DefaultServer>(c => c.RijndaelEncryptionService())
                    .AddMapping<MessageWithSecretData>(typeof(Receiver));
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

            public class ConfigureEncryption : IProvideConfiguration<RijndaelEncryptionServiceConfig>
            {
                public RijndaelEncryptionServiceConfig GetConfiguration()
                {
                    return new RijndaelEncryptionServiceConfig
                    {
                        Key = "gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"
                    };
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.RijndaelEncryptionService());
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

            public class ConfigureEncryption : IProvideConfiguration<RijndaelEncryptionServiceConfig>
            {
                public RijndaelEncryptionServiceConfig GetConfiguration()
                {
                    return new RijndaelEncryptionServiceConfig
                    {
                        Key = "adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6",
                        ExpiredKeys = new RijndaelExpiredKeyCollection
                        {
                            new RijndaelExpiredKey
                            {
                                Key = "gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"
                            }
                        }
                    };
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