namespace NServiceBus.Gateway.Config
{
    using System;
    using Persistence;
    using Sending;

    public class GatewayDefaults : IWantToRunBeforeConfigurationIsFinalized
    {
        public static Action DefaultPersistence = () => Configure.Instance.UseRavenGatewayPersister();

        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<GatewaySender>() ||
                Configure.Instance.Configurer.HasComponent<IPersistMessages>())
            {
                return;
            }

            DefaultPersistence();
        }
    }
}