namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using Config;
    using Config.ConfigurationSource;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_encryption : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_decrypted_message()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, context) => bus.SendLocal(new MessageWithSecretData
                        {
                            Secret = "betcha can't guess my secret",
                            SubProperty = new MySecretSubProperty {Secret = "My sub secret"},
                            CreditCards = new List<CreditCardDetails>
                                {
                                    new CreditCardDetails
                                        {
                                            ValidTo = DateTime.UtcNow.AddYears(1),
                                            Number = "312312312312312"
                                        },
                                    new CreditCardDetails
                                        {
                                            ValidTo = DateTime.UtcNow.AddYears(2),
                                            Number = "543645546546456"
                                        }
                                }
                        })))
                    .Done(c => c.Done)
                    .Repeat(r => r.For<AllSerializers>())
                    .Should(c =>
                        {
                            Assert.AreEqual("betcha can't guess my secret", c.Secret);
                            Assert.AreEqual("My sub secret", c.SubPropertySecret);
                            CollectionAssert.AreEquivalent(new List<string> { "312312312312312", "543645546546456" }, c.CreditCards);
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }

            public string Secret { get; set; }

            public string SubPropertySecret { get; set; }

            public List<string> CreditCards { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.RijndaelEncryptionService());
            }

            public class Handler : IHandleMessages<MessageWithSecretData>
            {
                public Context Context { get; set; }

                public void Handle(MessageWithSecretData message)
                {
                    Context.Secret = message.Secret.Value;

                    Context.SubPropertySecret = message.SubProperty.Secret.Value;

                    Context.CreditCards = new List<string>() { message.CreditCards[0].Number.Value, message.CreditCards[1].Number.Value };

                    Context.Done = true;
                }
            }
        }

        [Serializable]
        public class MessageWithSecretData : IMessage
        {
            public WireEncryptedString Secret { get; set; }
            public MySecretSubProperty SubProperty { get; set; }
            public List<CreditCardDetails> CreditCards { get; set; }
        }

        [Serializable]
        public class CreditCardDetails
        {
            public DateTime ValidTo { get; set; }
            public WireEncryptedString Number { get; set; }
        }

        [Serializable]
        public class MySecretSubProperty
        {
            public WireEncryptedString Secret { get; set; }
        }

        public class ConfigureEncryption: IProvideConfiguration<RijndaelEncryptionServiceConfig>
        {
            readonly RijndaelEncryptionServiceConfig rijndaelEncryptionServiceConfig = new RijndaelEncryptionServiceConfig { Key = "gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6" };

            public RijndaelEncryptionServiceConfig GetConfiguration()
            {
                return rijndaelEncryptionServiceConfig;
            }
        }
    }
}