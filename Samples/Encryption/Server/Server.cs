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
            Console.WriteLine("I know your secret - it's '" + message.Secret + "'.");
        }
    }
}
