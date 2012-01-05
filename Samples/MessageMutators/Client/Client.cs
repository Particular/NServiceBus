using System;
using MessageMutators;
using NServiceBus;
using Messages;

namespace Client
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client {}

    public class ConfigureMutators : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ValidationMessageMutator>(
                DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<TransportMessageCompressionMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }

    public class Runner : IWantToRunAtStartup
    {
        public void Run()
        {
            Console.Write("Press 's' to send a valid message, press 'e' to send a failed message. To exit, Ctrl + C\n" );

            string key;

            while ((key = Console.ReadLine()) != null)
            {
                if (key == "s" || key == "S")
                {
                    Bus.Send<CreateProductCommand>(m =>
                    {
                        m.ProductId = "XJ128";
                        m.ProductName= "Milk";
                        m.ListPrice = 4;
                        m.SellEndDate = new DateTime(2012, 1, 3);
                        // 7MB. MSMQ should throw an exception, but it will not since the buffer will be compressed 
                        // before it reaches MSMQ.
                        m.Image = new byte[1024 * 1024 * 7];
                    });
                }
                if (key == "e" || key == "E")
                {
                    Bus.Send<CreateProductCommand>(m =>
                    {
                        m.ProductId = "XJ128";
                        m.ProductName = "Milk Milk Milk Milk Milk";
                        m.ListPrice = 15;
                        m.SellEndDate = new DateTime(2011, 1, 3);
                        // 7MB. MSMQ should throw an exception, but it will not since the buffer will be compressed 
                        // before it reaches MSMQ.
                        m.Image = new byte[1024 * 1024 * 7];
                    });
                }
            }
        }

        public void Stop()
        {
        }

        public IBus Bus { get; set; }
    }
}
