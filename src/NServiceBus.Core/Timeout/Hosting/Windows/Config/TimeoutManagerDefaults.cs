namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using System;
    using Core;
    using NServiceBus.Config;

    public class TimeoutManagerDefaults : IWantToRunBeforeConfigurationIsFinalized
    {
        private static readonly Action DefaultPersistence = () => Configure.Instance.UseRavenTimeoutPersister();

        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<IPersistTimeouts>() && !Endpoint.IsSendOnly)
            {
                DefaultPersistence();
            }
        }
    }
}