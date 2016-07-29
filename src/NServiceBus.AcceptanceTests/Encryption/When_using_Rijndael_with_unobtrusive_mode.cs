namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_Rijndael_with_unobtrusive_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_decrypted_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(session => session.Send(new MessageWithSecretData
                {
                    EncryptedSecret = "betcha can't guess my secret",
                    SubProperty = new MySecretSubProperty
                    {
                        EncryptedSecret = "My sub secret"
                    },
                    CreditCards = new List<CreditCardDetails>
                    {
                        new CreditCardDetails
                        {
                            ValidTo = DateTime.UtcNow.AddYears(1),
                            EncryptedNumber = "312312312312312"
                        },
                        new CreditCardDetails
                        {
                            ValidTo = DateTime.UtcNow.AddYears(2),
                            EncryptedNumber = "543645546546456"
                        }
                    }
                })))
                .WithEndpoint<Receiver>()
                .Done(c => c.GetTheMessage || c.FailedMessages.Any())
                .Run();

            Assert.AreEqual("betcha can't guess my secret", context.Secret);
            Assert.AreEqual("My sub secret", context.SubPropertySecret);
            CollectionAssert.AreEquivalent(new List<string>
            {
                "312312312312312",
                "543645546546456"
            }, context.CreditCards);
        }

        static Dictionary<string, byte[]> Keys = new Dictionary<string, byte[]>
        {
            {"1st", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")}
        };

        public class Context : ScenarioContext
        {
            public bool GetTheMessage { get; set; }

            public string Secret { get; set; }

            public string SubPropertySecret { get; set; }

            public List<string> CreditCards { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions()
                        .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MessageWithSecretData).FullName)
                        .DefiningEncryptedPropertiesAs(t => t.Name.StartsWith("Encrypted"));

                    c.RijndaelEncryptionService("1st", Keys);
                }).AddMapping<MessageWithSecretData>(typeof(Receiver))
                    .ExcludeType<MessageWithSecretData>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions()
                        .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MessageWithSecretData).FullName)
                        .DefiningEncryptedPropertiesAs(t => t.Name.StartsWith("Encrypted"));

                    c.RijndaelEncryptionService("1st", Keys);
                });
            }

            public class Handler : IHandleMessages<MessageWithSecretData>
            {
                public Context Context { get; set; }

                public Task Handle(MessageWithSecretData message, IMessageHandlerContext context)
                {
                    Context.Secret = message.EncryptedSecret;

                    Context.SubPropertySecret = message.SubProperty.EncryptedSecret;

                    Context.CreditCards = new List<string>
                    {
                        message.CreditCards[0].EncryptedNumber,
                        message.CreditCards[1].EncryptedNumber
                    };

                    Context.GetTheMessage = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class MessageWithSecretData
        {
            public string EncryptedSecret { get; set; }
            public MySecretSubProperty SubProperty { get; set; }
            public List<CreditCardDetails> CreditCards { get; set; }
        }

        public class CreditCardDetails
        {
            public DateTime ValidTo { get; set; }
            public string EncryptedNumber { get; set; }
        }

        public class MySecretSubProperty
        {
            public string EncryptedSecret { get; set; }
        }
    }
}