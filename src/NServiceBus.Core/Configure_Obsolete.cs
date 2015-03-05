namespace NServiceBus
{
    using NServiceBus.Config;

    public partial class Configure
    {
        [ObsoleteEx(Message = "Remove when IWantToRunWhenConfigurationIsComplete is obsoleted.", RemoveInVersion = "7.0")]
        void RegisterWantToRunWhenConfigurationIsCompleteAsInstancePerCall()
        {
            ForAllTypes<IWantToRunWhenConfigurationIsComplete>(TypesToScan, t => container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
        }

        [ObsoleteEx(Message = "Remove when IWantToRunWhenConfigurationIsComplete is obsoleted.", RemoveInVersion = "7.0")]
        void RunWantToRunWhenConfigurationIsComplete()
        {
            foreach (var o in Builder.BuildAll<IWantToRunWhenConfigurationIsComplete>())
            {
                o.Run(this);
            }
        }
    }
}