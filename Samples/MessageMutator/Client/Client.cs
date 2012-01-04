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
            Configure.Instance.Configurer.ConfigureComponent<CompressionMutatorForASingleProperty>(
                DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<TransportMessageCompressionMutator>(
                DependencyLifecycle.InstancePerCall);

        }
    }

    public class Runner : IWantToRunAtStartup
    {
        public void Run()
        {
            Console.Write("Press 'c' to send a compressed message, press 't' to send a compressed transport message. To exit, Ctrl + C\n" );

            string key;

            while ((key = Console.ReadLine()) != null)
            {
                if (key == "c" || key == "C")
                {
                    Bus.Send<MessageWithDoubleAndByteArray>(m =>
                    {
                        m.MyDouble = 4;
                        // 7MB. MSMQ should throw an exception, but it will not since the buffer will be compressed 
                        // before it reaches MSMQ.
                        m.Buffer = new byte[1024*1024*7]; 
                        Console.WriteLine(
                            string.Format("Sending a message. Message double is {0}, message buffer length is {1}", 
                            m.MyDouble, m.Buffer.Length));
                    });
                }
                if (key == "t" || key == "T")
                {
                    //Define two messages of 5Mb each.
                    var messages = new MessageWithByteArray[]
                            {
                                new MessageWithByteArray { Buffer = new byte[1024 * 1024 * 5] }, 
                                new MessageWithByteArray { Buffer = new byte[1024 * 1024 * 5] }
                            };
                    Bus.Send(messages);
                    Console.WriteLine("Messages sent.  messages buffer length is {0}",
                        (messages[0].Buffer.Length + messages[0].Buffer.Length).ToString());
                }
            }
            return;
        }

        public void Stop()
        {
        }

        public IBus Bus { get; set; }
    }
}
