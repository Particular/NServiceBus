namespace NServiceBus.Unicast.Config
{
    using System.Linq;
    using NServiceBus.Config;

    internal class StartupRunners : NServiceBus.INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        public void Init()
        {
            Configure.TypesToScan
                .Where(
                    t =>
                    typeof(IWantToRunWhenTheBusStarts).IsAssignableFrom(t) && !t.IsInterface)
                .ToList()
                .ForEach(
                    type => Configure.Instance.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
        }

        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<UnicastBus>())
                return;

            Configure.Instance.Builder.Build<UnicastBus>().Started +=
                (obj, ev) => Configure.Instance.Builder.BuildAll<IWantToRunWhenTheBusStarts>().ToList()
                                      .ForEach(r => r.Run());
        }
    }
}