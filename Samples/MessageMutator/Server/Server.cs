using System;
using MessageMutators;
using Messages;
using NServiceBus;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    public class ConfigureMutators : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<CompressionMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }

    public class Handler : IHandleMessages<MessageWithDoubleAndByteArray>
    {
        public void Handle(MessageWithDoubleAndByteArray message)
        {
            Console.WriteLine(string.Format("Received a message. Message double is: {0}, buffer size is: {1}", message.MyDouble, message.Buffer.Length));
        }
    }
}
