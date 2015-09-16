namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Configuration;
    using NServiceBus.Config;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    public static class TimeoutPersistenceVersionCheckExtension
    {
        public static Configure SuppressOutdatedTimeoutPersistenceWarning(this Configure configure)
        {
            SettingsHolder.Set(TimeoutPersistenceVersionCheck.SuppressOutdatedTimeoutPersistenceWarning, true);
            return configure;
        }
    }

    internal class TimeoutPersistenceVersionCheck : IWantToRunWhenConfigurationIsComplete
    {
        internal const string SuppressOutdatedTimeoutPersistenceWarning = "NServiceBus/suppress-outdated-timeout-persistence-warning";

        IPersistTimeouts timeoutPersister;

        public TimeoutPersistenceVersionCheck(IPersistTimeouts timeoutPersister)
        {
            this.timeoutPersister = timeoutPersister;
        }

        public void Run()
        {
            var suppressDtc = SettingsHolder.Get<bool>("Transactions.SuppressDistributedTransactions");
            if (IsTransportSupportingDtc() && !suppressDtc)
            {
                // there is no issue with the timeout persistence when using dtc
                return;
            }

            if (timeoutPersister is IPersistTimeoutsV2)
            {
                return;
            }

            if (UserSuppressedWarning())
            {
                return;
            }

            throw new Exception("You are using an outdated timeout persistence which can lead to message loss! Please update the configured timeout persistence. You can suppress this warning by configuring your bus using 'config.SuppressOutdatedTimeoutPersistenceWarning()' or by adding 'NServiceBus/suppress-outdated-timeout-persistence-warning' with value 'true' to the appSettings section of your application configuration file.");
        }

        bool UserSuppressedWarning()
        {
            if (SettingsHolder.HasSetting(SuppressOutdatedTimeoutPersistenceWarning))
            {
                return SettingsHolder.GetOrDefault<bool>(SuppressOutdatedTimeoutPersistenceWarning);
            }

            var appSetting = ConfigurationManager.AppSettings[SuppressOutdatedTimeoutPersistenceWarning];
            if (appSetting != null)
            {
                return bool.Parse(appSetting);
            }

            return false;
        }

        bool IsTransportSupportingDtc()
        {
            var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");
            if (selectedTransport.HasSupportForDistributedTransactions.HasValue)
            {
                return selectedTransport.HasSupportForDistributedTransactions.Value;
            }

            return !selectedTransport.GetType().Name.Contains("RabbitMQ");
        }
    }
}