namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_Rijndael_with_multikey : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_decrypted_message()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given((bus, context) =>
                     {
                         bus.Send(new MessageWithSecretData
                         {
                             Secret = "betcha can't guess my secret",
                         });
                         bus.Send(new RegularMessage());
                     }))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.Done)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c =>
                    {
                        Assert.AreEqual("betcha can't guess my secret", c.Secret);
                        Assert.IsFalse(c.HasKeyOnRegularMessage.Value, "Key identifier header present in message without encrypted properties.");
                    })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string Secret { get; set; }
            public bool? HasKeyOnRegularMessage { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("1st", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")))
                    .AddMapping<MessageWithSecretData>(typeof(Receiver))
                    .AddMapping<RegularMessage>(typeof(Receiver))
                    ;
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                var key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
                var keys = new Dictionary<string, byte[]>
                {
                    {"2nd", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6") },
                    {"1st", key  }
                };

                var expiredKeys = new[] { key };
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("2nd", keys, expiredKeys));
            }

            public class Handler : IHandleMessages<MessageWithSecretData>, IHandleMessages<RegularMessage>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MessageWithSecretData message)
                {
                    Context.Secret = message.Secret.Value;
                    Context.Done = true;
                }

                public void Handle(RegularMessage message)
                {
                    var hasKey = null != Bus.GetMessageHeader(message, Headers.RijndaelKeyIdentifier);
                    Context.HasKeyOnRegularMessage = hasKey;
                }
            }
        }

        [Serializable]
        public class MessageWithSecretData : IMessage
        {
            public WireEncryptedString Secret { get; set; }
        }

        [Serializable]
        public class RegularMessage : IMessage
        {
        }
    }
}
