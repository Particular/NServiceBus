namespace NServiceBus.Audit.Config
{
    using NServiceBus.Settings;

    public class DefaultAuditSettings : ISetDefaultSettings
    {
        public DefaultAuditSettings()
        {
            SettingsHolder.SetDefault<AuditFilters>(new AuditFilters());
        }
    }
}
