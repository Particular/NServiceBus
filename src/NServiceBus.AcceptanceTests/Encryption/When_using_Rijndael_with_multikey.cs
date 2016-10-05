namespace NServiceBus.AcceptanceTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_Rijndael_with_multikey : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_receive_decrypted_message()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((session, context) => session.Send(new MessageWithSecretData
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
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("1st", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")))
                    .AddMapping<MessageWithSecretData>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                var key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
                var keys = new Dictionary<string, byte[]>
                {
                    {"2nd", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")},
                    {"1st", key}
                };

                var expiredKeys = new[]
                {
                    key
                };
                EndpointSetup<DefaultServer>(builder => builder.RijndaelEncryptionService("2nd", keys, expiredKeys));
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