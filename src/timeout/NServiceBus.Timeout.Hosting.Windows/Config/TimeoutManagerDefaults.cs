namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using System;
    using NServiceBus.Config;
    using Core;


    public class TimeoutManagerDefaults : IWantToRunWhenConfigurationIsComplete
    {
        public static Action DefaultPersistence = () => Configure.Instance.UseRavenTimeoutPersister();

        public void Run()
        {
            if (Configure.Instance.IsTimeoutManagerEnabled() && !Configure.Instance.Configurer.HasComponent<IPersistTimeouts>())
                DefaultPersistence();
        }
    }
}