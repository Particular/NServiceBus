﻿namespace NServiceBus.Setup.Windows.Dtc
{
    using System;
    using System.Collections.Generic;
    using System.ServiceProcess;
    using Microsoft.Win32;

    public class DtcSetup
    {
        /// <summary>
        /// Checks that the MSDTC service is running and configured correctly, and if not
        /// takes the necessary corrective actions to make it so.
        /// </summary>
        public static bool StartDtcIfNecessary(bool allowReconfiguration = false)
        {
            if (DoesSecurityConfigurationRequireRestart(allowReconfiguration))
            {
                if (!allowReconfiguration)
                    return false;

                ProcessUtil.ChangeServiceStatus(Controller, ServiceControllerStatus.Stopped, Controller.Stop);
            }

            if (!allowReconfiguration && Controller.Status != ServiceControllerStatus.Running)
            {
                Console.Out.WriteLine("MSDTC isn't currently running and needs to be started");
                return false;
            }

            ProcessUtil.ChangeServiceStatus(Controller, ServiceControllerStatus.Running, Controller.Start);

            return true;
        }

        static bool DoesSecurityConfigurationRequireRestart(bool doChanges)
        {
            Console.WriteLine("Checking if DTC is configured correctly.");
            
            var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\MSDTC\Security", doChanges);
            if (key == null)
                throw new InvalidOperationException("MSDTC could not be found in the registry. Cannot continue.");

            var needToChange = false;
            foreach (var val in RegValues)
                if ((int)key.GetValue(val) == 0)
                    if (doChanges)
                    {
                        
                        Console.WriteLine("DTC not configured correctly. Going to fix. This will require a restart of the DTC service.");

                        key.SetValue(val, 1, RegistryValueKind.DWord);
                    
                        Console.WriteLine("DTC configuration fixed.");
                    }
                    else
                        needToChange = true;

            key.Close();

            return needToChange;
        }

        static readonly ServiceController Controller = new ServiceController { ServiceName = "MSDTC", MachineName = "." };
        static readonly List<string> RegValues = new List<string>(new[] { "NetworkDtcAccess", "NetworkDtcAccessOutbound", "NetworkDtcAccessTransactions", "XaTransactions" });
    }
}