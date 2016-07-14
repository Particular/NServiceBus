namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Configuration.AdvanceExtensibility;
    using Settings;
    using Transport;

    /// <summary>
    ///
    /// </summary>
    public static class RecoverabilityExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static RecoverabilitySettings Recoverability(this EndpointConfiguration configuration)
        {
            return new RecoverabilitySettings(configuration.GetSettings());
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class RecoverabilitySettings : ExposeSettings
    {
        internal const string RecoverabilityPolicySettingsKey = "Recoverability.Policy";

        internal RecoverabilitySettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="policy"></param>
        public void PolicyOverride(Func<RecoverabilityAction, ErrorContext, RecoverabilityAction> policy)
        {
            this.GetSettings().Set(RecoverabilityPolicySettingsKey, policy);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="mutator"></param>
        public void MutateExceptionHeaders(Action<Dictionary<string, string>> mutator)
        {
            this.GetSettings().Set("Recoverability.ExceptionHeadersMutator", mutator);
        }
    }
}