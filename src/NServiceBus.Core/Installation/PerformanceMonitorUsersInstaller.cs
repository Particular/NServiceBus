namespace NServiceBus.Installation
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Environments;
    using Logging;

    /// <summary>
    /// Add the identity to the 'Performance Monitor Users' local group 
    /// </summary>
    public class PerformanceMonitorUsersInstaller : INeedToInstallSomething<Windows>
    {
        static ILog logger = LogManager.GetLogger<PerformanceMonitorUsersInstaller>();

        public void Install(string identity,Configure configure)
        {
            //did not use DirectoryEntry to avoid a ref to the DirectoryServices.dll
            try
            {
                if (!ElevateChecker.IsCurrentUserElevated())
                {
                    logger.InfoFormat(@"Did not attempt to add user '{0}' to group 'Performance Monitor Users' since process is not running with elevate privileges. Processing will continue. To manually perform this action run the following command from an admin console:
net localgroup ""Performance Monitor Users"" ""{0}"" /add", identity);
                    return;
                }
                StartProcess(identity);
            }
            catch (Exception win32Exception)
            {
                var message = string.Format(
                    @"Failed adding user '{0}' to group 'Performance Monitor Users' due to an Exception. 
To help diagnose the problem try running the following command from an admin console:
net localgroup ""Performance Monitor Users"" ""{0}"" /add", identity);
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
                                Arguments = string.Format("localgroup \"Performance Monitor Users\" \"{0}\" /add", identity),
                                FileName = "net",
                                WorkingDirectory = Path.GetTempPath()
                            };
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit(5000);

                if (process.ExitCode == 0)
                {
                    logger.Info(string.Format("Added user '{0}' to group 'Performance Monitor Users'.", identity));
                    return;
                }
                var error = process.StandardError.ReadToEnd();
                if (IsAlreadyAMemberError(error))
                {
                    logger.Info(string.Format("Skipped adding user '{0}' to group 'Performance Monitor Users' because the user is already in group.", identity));
                    return;
                }
                if (IsGroupDoesNotExistError(error))
                {
                    logger.Info(string.Format("Skipped adding user '{0}' to group 'Performance Monitor Users' because the group does not exist.", identity));
                    return;
                }
                var message = string.Format(
                    @"Failed to add user '{0}' to group 'Performance Monitor Users'. 
Error: {1}
To help diagnose the problem try running the following command from an admin console:
net localgroup ""Performance Monitor Users"" ""{0}"" /add", identity, error);
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