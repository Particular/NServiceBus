using System;
using NServiceBus;
using Messages;

namespace Client
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client {}

    public class SecurityConfig : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService();
        }
    }

    public class Runner : IWantToRunWhenBusStartsAndStops
    {
        public void Start()
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
