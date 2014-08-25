namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Principal;
    using Logging;
    using NServiceBus.Installation;

    /// <summary>
    /// Add the identity to the 'Performance Monitor Users' local group 
    /// </summary>
    class PerformanceMonitorUsersInstaller : INeedToInstallSomething
    {
        static ILog logger = LogManager.GetLogger<PerformanceMonitorUsersInstaller>();
        static string BuiltinPerformanceMonitoringUsersName = new SecurityIdentifier(WellKnownSidType.BuiltinPerformanceMonitoringUsersSid, null).Translate(typeof(NTAccount)).ToString();
        
        public void Install(string identity, Configure config)
        {
            //did not use DirectoryEntry to avoid a ref to the DirectoryServices.dll
            try
            {
                if (!ElevateChecker.IsCurrentUserElevated())
                {
                    logger.InfoFormat(@"Did not attempt to add user '{0}' to group '{1}' since process is not running with elevate privileges. Processing will continue. To manually perform this action run the following command from an admin console:
net localgroup ""{1}"" ""{0}"" /add", identity, BuiltinPerformanceMonitoringUsersName);
                    return;
                }
                StartProcess(identity);
            }
            catch (Exception win32Exception)
            {
                var message = string.Format(
                    @"Failed adding user '{0}' to group '{1}' due to an Exception. 
To help diagnose the problem try running the following command from an admin console:
net localgroup ""{1}"" ""{0}"" /add", identity, BuiltinPerformanceMonitoringUsersName);
                logger.Warn(message, win32Exception);
            }
        }


        void StartProcess(string identity)
        {
            //net localgroup "Performance Monitor Users" "{user account}" /add
            var startInfo = new ProcessStartInfo
                            {
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardError = true,
                                Arguments = string.Format("localgroup \"{1}\" \"{0}\" /add", identity, BuiltinPerformanceMonitoringUsersName),
                                FileName = "net",
                                WorkingDirectory = Path.GetTempPath()
                            };
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit(5000);

                if (process.ExitCode == 0)
                {
                    logger.Info(string.Format("Added user '{0}' to group '{1}'.", identity, BuiltinPerformanceMonitoringUsersName));
                    return;
                }
                var error = process.StandardError.ReadToEnd();
                if (IsAlreadyAMemberError(error))
                {
                    logger.Info(string.Format("Skipped adding user '{0}' to group '{1}' because the user is already in group.", identity, BuiltinPerformanceMonitoringUsersName));
                    return;
                }
                if (IsGroupDoesNotExistError(error))
                {
                    logger.Info(string.Format("Skipped adding user '{0}' to group '{1}' because the group does not exist.", identity, BuiltinPerformanceMonitoringUsersName));
                    return;
                }
                var message = string.Format(
                    @"Failed to add user '{0}' to group '{2}'. 
Error: {1}
To help diagnose the problem try running the following command from an admin console:
net localgroup ""{2}"" ""{0}"" /add", identity, error, BuiltinPerformanceMonitoringUsersName);
                logger.Info(message);
            }
        }

        bool IsAlreadyAMemberError(string error)
        {
            return error.Contains("1378");
        }

        bool IsGroupDoesNotExistError(string error)
        {
            //required since 'Performance Monitor Users' does not exist on all windows OS.
            return error.Contains("1376");
        }
    }
}