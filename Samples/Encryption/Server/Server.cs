using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .StructureMapBuilder()
                .RijndaelEncryptionService();
        }
    }

    public class Handler : IHandleMessages<MessageWithSecretData>
    {
        public void Handle(MessageWithSecretData message)
        {
            Console.Out.WriteLine("I know your secret - it's '" + message.Secret + "'");

            if (message.SubProperty != null)
                Console.Out.WriteLine("SubSecret: " + message.SubProperty.Secret);


            if (message.CreditCards != null)
                foreach (var creditCard in message.CreditCards)
                    Console.Out.WriteLine("CreditCard: {0} is valid to {1}", creditCard.Number.Value, creditCard.ValidTo);
        }
    }
}
