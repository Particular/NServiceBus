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
            Configure.Instance.Configurer.ConfigureComponent<CompressionMutatorForASingleProperty>(
                DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<TransportMessageCompressionMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }

    public class Handler : IHandleMessages<MessageWithDoubleAndByteArray>, IHandleMessages<MessageWithByteArray>
    {
        public void Handle(MessageWithDoubleAndByteArray message)
        {
            Console.WriteLine(string.Format("Received a {0} message. Message double is: {1}, buffer size is: {2}", 
                message.GetType(), message.MyDouble, message.Buffer.Length));
        }

        public void Handle(MessageWithByteArray message)
        {
            Console.WriteLine(string.Format("Received a {0} message. Message buffer size is: {1}", 
                message.GetType(), message.Buffer.Length));
        }
    }
}
