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
            Configure.Instance.Configurer.ConfigureComponent<MultiplierMutator>(
                DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<CompressionMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }

    public class Runner : IWantToRunAtStartup
    {
        public void Run()
        {
            Console.WriteLine("Press 'Enter' to send a message.");
            while (Console.ReadLine() != null)
            {
                Bus.Send<MessageWithDoubleAndByteArray>(m =>
                    {
                        m.MyDouble = 4;
                        m.Buffer = new byte[1024*1024*7]; // 7MB. MSMQ should throw an exception, but it will not since the buffer will be compressed before it reaches MSMQ.
                        Console.WriteLine(string.Format("Sending a message. Message double is {0}, message buffer length is {1}", m.MyDouble, m.Buffer.Length));
                    });
                Console.WriteLine("Message sent.");
            }
        }

        public void Stop()
        {
        }

        public IBus Bus { get; set; }
    }
}
