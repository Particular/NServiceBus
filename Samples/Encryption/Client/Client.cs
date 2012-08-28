using System;
using NServiceBus;
using Messages;

namespace Client
{
    using NServiceBus.Encryption.Config;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client {}

    public class SecurityConfig : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService()
             .DisableCompatibilityWithNSB2();//remove this line if you need to be compatible with a 2.X server
        }
    }

    public class Runner : IWantToRunAtStartup
    {
        public void Run()
        {
            Console.WriteLine("Press 'Enter' to send a message.");
            while (Console.ReadLine() != null)
            {
                Bus.Send<MessageWithSecretData>(m => m.Secret = "betcha can't guess my secret");
            }
        }

        public void Stop()
        {
        }

        public IBus Bus { get; set; }
    }
}
