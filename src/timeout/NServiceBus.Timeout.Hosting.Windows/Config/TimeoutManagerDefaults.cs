namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using System;
    using Core;


    public class TimeoutManagerDefaults : IWantToRunBeforeConfigurationIsFinalized
    {
        public static Action DefaultPersistence = () => Configure.Instance.UseRavenTimeoutPersister();

        public void Run()
        {
            if (Configure.Instance.IsTimeoutManagerEnabled() && !Configure.Instance.Configurer.HasComponent<IPersistTimeouts>())
            {
                DefaultPersistence();
            }
        }
    }
}