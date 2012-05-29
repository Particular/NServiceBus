namespace NServiceBus.Unicast.Config
{
    using System.Linq;
    using NServiceBus.Config;

    internal class StartupRunners : INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        public void Init()
        {
#pragma warning disable 618
            Configure.TypesToScan
                .Where(
                    t =>
                    (typeof (IWantToRunWhenTheBusStarts).IsAssignableFrom(t) ||
                     typeof (NServiceBus.IWantToRunWhenTheBusStarts).IsAssignableFrom(t)) && !t.IsInterface)
                .ToList()
                .ForEach(
                    type => Configure.Instance.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
#pragma warning restore 618
        }

        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<UnicastBus>())
                return;

            Configure.Instance.Builder.Build<UnicastBus>().Started +=
                (obj, ev) =>
                    {
#pragma warning disable 618
                        Configure.Instance.Builder.BuildAll<IWantToRunWhenTheBusStarts>().ToList()
                            .ForEach(r => r.Run());
#pragma warning restore 618
                        Configure.Instance.Builder.BuildAll<NServiceBus.IWantToRunWhenTheBusStarts>().ToList()
                            .ForEach(r => r.Run());
                    };
        }
    }
}