using System;
using System.Collections.Generic;
using System.ServiceProcess;
using Common.Logging;
using Microsoft.Win32;

namespace NServiceBus.Utils
{
    /// <summary>
    /// Utility class for working with DTC.
    /// </summary>
    public static class DtcUtil
    {
        /// <summary>
        /// Checks that the MSDTC service is running and configured correctly, and if not
        /// takes the necessary corrective actions to make it so.
        /// </summary>
        public static void StartDtcIfNecessary()
        {
            if (DoesSecurityConfigurationRequireRestart())
                ProcessUtil.ChangeServiceStatus(Controller, ServiceControllerStatus.Stopped, Controller.Stop);

            ProcessUtil.ChangeServiceStatus(Controller, ServiceControllerStatus.Running, Controller.Start);

            Logger.Debug("DTC is good.");
        }

        private static bool DoesSecurityConfigurationRequireRestart()
        {
            Logger.Debug("Checking that DTC is configured correctly.");

            if (DoesSecurityConfigurationRequireRestart(false))
            {
                Logger.Debug("DTC not configured correctly. Going to fix. This will require a restart of the DTC service.");

                DoesSecurityConfigurationRequireRestart(true);

                Logger.Debug("DTC configuration fixed.");

                return true;
            }

            Logger.Debug("DTC is configured correctly.");
            return false;
        }

        private static bool DoesSecurityConfigurationRequireRestart(bool doChanges)
        {
            var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\MSDTC\Security", doChanges);
            if (key == null)
                throw new InvalidOperationException("MSDTC could not be found in the registry. Cannot continue.");

            var needToChange = false;
            foreach (var val in RegValues)
                if ((int)key.GetValue(val) == 0)
                    if (doChanges)
                        key.SetValue(val, 1, RegistryValueKind.DWord);
                    else
                        needToChange = true;

            key.Close();

            return needToChange;
        }

        private static readonly ServiceController Controller = new ServiceController { ServiceName = "MSDTC", MachineName = "." };
        private static readonly List<string> RegValues = new List<string>(new[] {"NetworkDtcAccess", "NetworkDtcAccessOutbound", "NetworkDtcAccessTransactions", "XaTransactions"});

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Utils");
    }
}
