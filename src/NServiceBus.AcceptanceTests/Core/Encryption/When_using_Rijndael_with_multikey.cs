// disable obsolete warnings. Tests will be removed in next major version
#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.Core.Encryption
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_Rijndael_with_multikey : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_decrypted_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((session, ctx) => session.Send(new MessageWithSecretData
                {
                    Secret = "betcha can't guess my secret"
                })))
                .WithEndpoint<Receiver>()
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual("betcha can't guess my secret", context.Secret);
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


        public class MessageWithSecretData : IMessage
        {
            public WireEncryptedString Secret { get; set; }
        }
    }
}
#pragma warning restore CS0618