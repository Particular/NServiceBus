namespace NServiceBus.Timeout.Core
{
    using NServiceBus.Config;
    using System;
    using System.Configuration;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// 
    /// </summary>
    public static class TimeoutPersistenceVersionCheckExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static BusConfiguration SuppressOutdatedTimeoutPersistenceWarning(this BusConfiguration configure)
        {
            configure.Settings.Set(TimeoutPersistenceVersionCheck.SuppressOutdatedTimeoutPersistenceWarning, true);
            return configure;
        }
    }

    internal class TimeoutPersistenceVersionCheck : IWantToRunWhenConfigurationIsComplete
    {
        internal const string SuppressOutdatedTimeoutPersistenceWarning = "NServiceBus/suppress-outdated-timeout-persistence-warning";
        
        readonly IBuilder builder;
        internal SettingsHolder settingsHolder;

        public TimeoutPersistenceVersionCheck(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Run(Configure config)
        {
            settingsHolder = config.Settings;
            var suppressDtc = settingsHolder.Get<bool>("Transactions.SuppressDistributedTransactions");
            if (IsTransportSupportingDtc() && !suppressDtc)
            {
                // there is no issue with the timeout persistence when using dtc
                return;
            }

            var timeoutPersister = TryResolveTimeoutPersister();
            if (timeoutPersister == null)
            {
                // no timeouts used
                return;
            }

            if (!(timeoutPersister is IPersistTimeoutsV2) && !UserSuppressedTimeoutPersistenceWarning())
            {
                throw new Exception("You are using an outdated timeout persistence which can lead to message loss! Please update the configured timeout persistence. You can suppress this warning by configuring your bus using 'config.SuppressOutdatedTimeoutPersistenceWarning()' or by adding 'NServiceBus/suppress-outdated-timeout-persistence-warning' with value 'true' to the appSettings section of your application configuration file.");
            }
        }

        IPersistTimeouts TryResolveTimeoutPersister()
        {
            IPersistTimeouts timeoutPersister = null;
            try
            {
                timeoutPersister = builder.Build<IPersistTimeouts>();
            }
            catch (Exception)
            {
                // catch potential DI exception when interface not registered.
            }

            return timeoutPersister;
        }

        bool UserSuppressedTimeoutPersistenceWarning()
        {
            if (settingsHolder.HasSetting(SuppressOutdatedTimeoutPersistenceWarning))
            {
                return settingsHolder.GetOrDefault<bool>(SuppressOutdatedTimeoutPersistenceWarning);
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
            var selectedTransport = settingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transports.TransportDefinition");
            if (selectedTransport.HasSupportForDistributedTransactions.HasValue)
            {
                return selectedTransport.HasSupportForDistributedTransactions.Value;
            }

            return !selectedTransport.GetType().Name.Contains("RabbitMQ");
        }
    }
} 
