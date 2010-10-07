using NServiceBus.Config;


namespace NServiceBus.Faults.Forwarder.Config
{
    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            if (Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                return;

            Configure.Instance.MessageForwardingInCaseOfFault();
        }
    }
}