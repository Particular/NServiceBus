namespace NServiceBus
{
    using System;
    using Settings;
    using Transport;

    static class RecoverabilitySettingsExtensions
    {
        public static Func<RecoverabilityAction, ErrorContext, RecoverabilityAction> GetCustomRecoverabilityPolicy(this ReadOnlySettings settings)
        {
            return settings.Get<Func<RecoverabilityAction, ErrorContext, RecoverabilityAction>>(RecoverabilitySettings.RecoverabilityPolicySettingsKey);
        }
    }
}