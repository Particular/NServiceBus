namespace NServiceBus.Unicast.Config
{
    using Settings;

    class SetUnicastBusConfigurationDefaults : ISetDefaultSettings
    {
        public SetUnicastBusConfigurationDefaults()
        {
            SettingsHolder.Instance.SetDefault("UnicastBus.AutoSubscribe", true);
        }
    }
}