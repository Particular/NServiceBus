namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Encryption;
    using NUnit.Framework;

    public class When_using_encryption_with_custom_service : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_decrypted_message()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new MessageWithSecretData
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
                    .Done(c => c.GotTheMessage)
                    .Run();

            Assert.AreEqual("betcha can't guess my secret", context.Secret);
            Assert.AreEqual("My sub secret", context.SubPropertySecret);
            CollectionAssert.AreEquivalent(new List<string> { "312312312312312", "543645546546456" }, context.CreditCards);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }

            public string Secret { get; set; }

            public string SubPropertySecret { get; set; }

            public List<string> CreditCards { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(builder => builder.RegisterEncryptionService(_ => new MyEncryptionService()));
            }

            public class Handler : IHandleMessages<MessageWithSecretData>
            {
                public Context Context { get; set; }

                public void Handle(MessageWithSecretData message)
                {
                    Context.Secret = message.Secret.Value;

                    Context.SubPropertySecret = message.SubProperty.Secret.Value;

                    Context.CreditCards = new List<string>
                    {
                        message.CreditCards[0].Number.Value, 
                        message.CreditCards[1].Number.Value
                    };

                    Context.GotTheMessage = true;
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

        public class MyEncryptionService: IEncryptionService
        {
            public EncryptedValue Encrypt(string value)
            {
                return new EncryptedValue
                {
                    EncryptedBase64Value = new string(value.Reverse().ToArray())
                };
            }

            public string Decrypt(EncryptedValue encryptedValue)
            {
                return new string(encryptedValue.EncryptedBase64Value.Reverse().ToArray());
            }
        }
    }
}