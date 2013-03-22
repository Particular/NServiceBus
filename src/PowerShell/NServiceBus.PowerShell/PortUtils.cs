namespace NServiceBus.PowerShell
{
    using System.Linq;
    using System.Net.NetworkInformation;

    public class PortUtils
    {
        public static int FindAvailablePort(int startPort)
        {
            var activeTcpListeners = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners();

            for (var port = startPort; port < startPort + 1024; port++)
            {
                var portCopy = port;
                if (activeTcpListeners.All(endPoint => endPoint.Port != portCopy))
                {
                    return port;
                }
            }

            return startPort;
        }

        public static bool IsPortAvailable(int port)
        {
            var activeTcpListeners = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners();


            return !activeTcpListeners.Any(ip => ip.Port == port);
        } 
    }
}