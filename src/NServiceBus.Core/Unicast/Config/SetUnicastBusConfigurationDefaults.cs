namespace NServiceBus.Unicast.Config
{
    using Settings;

    class SetUnicastBusConfigurationDefaults : ISetDefaultSettings
    {
        public SetUnicastBusConfigurationDefaults()
        {
            SettingsHolder.SetDefault("UnicastBus.AutoSubscribe", true);
        }
    }
}