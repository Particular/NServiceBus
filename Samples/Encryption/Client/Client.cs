using System;
using NServiceBus;
using Messages;

namespace Client
{
    using System.Collections.Generic;
    using NServiceBus.Encryption.Config;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client {}

    public class SecurityConfig : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService();
             //.DisableCompatibilityWithNSB2();//uncomment this line to turn off compatibility with2.X endpoints
        }
    }

    public class Runner : IWantToRunAtStartup
    {
        public void Run()
        {
            Console.WriteLine("Press 'Enter' to send a message.");
            while (Console.ReadLine() != null)
            {
                Bus.Send<MessageWithSecretData>(m =>
                                                    {
                                                        m.Secret = "betcha can't guess my secret";
                                                        m.SubProperty = new MySecretSubProperty {Secret = "My sub secret"};
                                                        m.CreditCards = new List<CreditCardDetails>
                                                                                  {
                                                                                      new CreditCardDetails{ValidTo = DateTime.UtcNow.AddYears(1), Number = "312312312312312"},
                                                                                      new CreditCardDetails{ValidTo = DateTime.UtcNow.AddYears(2), Number = "543645546546456"}
                                                                                  };
                                                    });
            }
        }

        public void Stop()
        {
        }

        public IBus Bus { get; set; }
    }
}
