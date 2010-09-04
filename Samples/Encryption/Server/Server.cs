using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    public class SecurityConfig : IWantCustomInitialization
    {
        public void Init()
        {
            NServiceBus.Configure.Instance.RijndaelEncryptionService();
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
