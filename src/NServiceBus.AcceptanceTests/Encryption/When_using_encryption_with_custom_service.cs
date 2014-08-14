namespace NServiceBus.AcceptanceTests.Encryption
{
    using System.Linq;
    using NServiceBus.Encryption;
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_encryption_with_custom_service : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_decrypted_message()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, context) => bus.SendLocal(new MessageWithSecretData
                        {
                            Secret = "betcha can't guess my secret"
                        })))
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
        
        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Configurer.RegisterSingleton<IEncryptionService>(new MyEncryptionService()));
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


        public class MyEncryptionService : IEncryptionService
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