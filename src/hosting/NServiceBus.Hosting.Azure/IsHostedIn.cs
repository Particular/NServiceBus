using System.Diagnostics;

namespace NServiceBus.Hosting.Azure
{
    public static class IsHostedIn
    {
        public static bool ChildHostProcess()
        {
            var currentProcess = Process.GetCurrentProcess();
            return currentProcess.ProcessName == "NServiceBus.Hosting.Azure.HostProcess";
        }
    }
}