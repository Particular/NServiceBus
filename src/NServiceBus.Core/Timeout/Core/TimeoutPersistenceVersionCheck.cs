namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Configuration;
    using NServiceBus.Config;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class TimeoutPersistenceVersionCheck : IWantToRunWhenConfigurationIsComplete
    {
        internal const string SuppressOutdatedTimeoutDispatchWarning = "NServiceBus/suppress-outdated-timeout-dispatch-warning";

        public void Run()
        {
            if (IsTransportSupportingDtc() && !SettingsHolder.Get<bool>("Transactions.SuppressDistributedTransactions"))
            {
                // there is no issue with the timeout persistence when using dtc
                return;
            }

            if (UserSuppressedOutdatedDispatchWarning())
            {
                return;
            }

            throw new Exception("You are using an outdated timeout dispatch implementation which can lead to message loss! Please update NServiceBus package to version 4.4.8 or higher. You can suppress this warning by configuring your bus using 'config.SuppressOutdatedTimeoutDispatchWarning()' or by adding 'NServiceBus/suppress-outdated-timeout-dispatch-warning' with value 'true' to the appSettings section of your application configuration file.");
        }

        bool UserSuppressedOutdatedDispatchWarning()
        {
            if (SettingsHolder.HasSetting(SuppressOutdatedTimeoutDispatchWarning))
            {
                return SettingsHolder.GetOrDefault<bool>(SuppressOutdatedTimeoutDispatchWarning);
            }

            var appSetting = ConfigurationManager.AppSettings[SuppressOutdatedTimeoutDispatchWarning];
            if (appSetting != null)
            {
                return bool.Parse(appSetting);
            }

            return false;
        }

        bool IsTransportSupportingDtc()
        {
            var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");
            return (selectedTransport.GetType().Name.ToLower().Contains("msmq") || selectedTransport.GetType().Name.ToLower().Contains("sql"));
        }
    }
}
