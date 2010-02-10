using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Queuing.Msmq.Config
{
    public static class ConfigureMsmqMessageQueue
    {
        public static void MsmqMessageQueue(this Configure config)
        {
            config.Configurer.ConfigureComponent<MsmqMessageQueue>(ComponentCallModelEnum.Singleton);
        }
    }
}
