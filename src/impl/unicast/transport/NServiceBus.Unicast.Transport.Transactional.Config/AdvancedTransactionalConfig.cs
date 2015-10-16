using System;
using System.Configuration;

namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    public static class AdvancedTransactionalConfig
    {
        static string SuppressOutdatedTimeoutDispatchSetting = "NServiceBus/suppress-outdated-timeout-dispatch-warning";

        /// <summary>
        /// Suppress the use of DTC. Can be combined with IsTransactional to turn off
        /// the DTC but still use the retries
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure SupressDTC(this Configure config)
        {
            if (UserSuppressedOutdatedDispatchWarning())
            {
                Bootstrapper.SupressDTC = true;
                return config;
            }

            var exceptionMessage = string.Format(
                "You are using an outdated timeout dispatch implementation which can lead to message loss! " +
                "Please update NServiceBus package to version 4.4.8 or higher. " +
                "You can suppress this warning by calling 'SuppressOutdatedTimeoutDispatchWarning()' before 'SuppressDTC()' (e.g. 'configure.SuppressOutdatedTimeoutDispatchWarning().SupressDTC()')" +
                " or by adding key '{0}' with value 'true' to the appSettings section of your application configuration file.", SuppressOutdatedTimeoutDispatchSetting);
            throw new Exception(exceptionMessage);
        }
        
        public static Configure SuppressOutdatedTimeoutDispatchWarning(this Configure config)
        {
            Bootstrapper.SuppressOutdatedTimeoutDispatchWarning = true;
            return config;
        }

        static bool UserSuppressedOutdatedDispatchWarning()
        {
            return Bootstrapper.SuppressOutdatedTimeoutDispatchWarning || UserSuppressedOutdatedDispatchWarningUsingAppConfig();
        }

        static bool UserSuppressedOutdatedDispatchWarningUsingAppConfig()
        {
            var appSetting = ConfigurationManager.AppSettings[SuppressOutdatedTimeoutDispatchSetting];
            if (appSetting != null)
            {
                return bool.Parse(appSetting);
            }
            return false;
        }
    }
}