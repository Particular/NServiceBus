namespace NServiceBus.Audit.Config
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Settings;

    public static class AuditSettingsExtensions
    {
        public static FeatureSettings Audit(this FeatureSettings settings, Action<AuditFilters> customSettings)
        {
            var auditFilters = new AuditFilters();
            customSettings(auditFilters);
            SettingsHolder.Set<AuditFilters>(auditFilters);

            return settings;
        }
    }
}