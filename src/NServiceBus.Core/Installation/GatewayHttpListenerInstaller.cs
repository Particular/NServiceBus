namespace NServiceBus.Installation
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Environments;
    using Features;
    using Gateway.Receiving;
    using Logging;

    /// <summary>
    /// Allows the identity to host http listeners for the <see cref="Gateway"/>
    /// </summary>
    public class GatewayHttpListenerInstaller : INeedToInstallSomething<Windows>
    {
        static ILog logger = LogManager.GetLogger(typeof(GatewayHttpListenerInstaller));
        public IManageReceiveChannels ChannelManager { get; set; }

        public void Install(string identity)
        {
            if (!Feature.IsEnabled<Gateway>())
            {
                return;
            }

            if (Environment.OSVersion.Version.Major <= 5)
            {
                logger.InfoFormat(
@"Did not attempt to grant user '{0}' HttpListener permissions since you are running an old OS. Processing will continue. 
To manually perform this action run the following command for each port from an admin console:
httpcfg set urlacl /u http://+:PORT/ /a D:(A;;GX;;;""{0}"")", identity);
                return;
            }
            if (!ElevateChecker.IsCurrentUserElevated())
            {
                logger.InfoFormat(
@"Did not attempt to grant user '{0}' HttpListener permissions since process is not running with elevate privileges. Processing will continue. 
To manually perform this action run the following command for each port from an admin console:
netsh http add urlacl url=http://+:PORT/ user=""{0}""", identity);
                return;
            }

            foreach (var receiveChannel in ChannelManager.GetReceiveChannels())
            {
                var uri = new Uri(receiveChannel.Address);
                if (!uri.Scheme.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                var port = uri.Port;
                try
                {
                      StartNetshProcess(identity, port);
                }
                catch (Exception exception)
                {
                    var message = string.Format(
@"Failed to grant to grant user '{0}' HttpListener permissions due to an Exception. Processing will continue.  
To help diagnose the problem try running the following command from an admin console:
netsh http add urlacl url=http://+:{0}/ user=""{1}""", port, identity);
                    logger.Warn(message, exception);
                }
            }
        }


        static internal void StartNetshProcess(string identity, int port)
        {
            var startInfo = new ProcessStartInfo
                            {
                                CreateNoWindow = true,
                                Verb = "runas",
                                UseShellExecute = false,
                                RedirectStandardError = true,
                                Arguments = string.Format(@"http add urlacl url=http://+:{0}/ user=""{1}""", port, identity),
                                FileName = "netsh",
                                WorkingDirectory = Path.GetTempPath()
                            };
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit(5000);

                if (process.ExitCode == 0)
                {
                    logger.Info(string.Format("Granted user '{0}' HttpListener permissions for port {1}.", identity,port));
                    return;
                }
                var error = process.StandardError.ReadToEnd();
                var message = string.Format(
@"Failed to grant to grant user '{0}' HttpListener permissions. Processing will continue. 
Error: {1}
To help diagnose the problem try running the following command from an admin console:
netsh http add urlacl url=http://+:{1}/ user=""{2}""", error, port, identity);
                logger.Warn(message);
            }
        }

    }

}