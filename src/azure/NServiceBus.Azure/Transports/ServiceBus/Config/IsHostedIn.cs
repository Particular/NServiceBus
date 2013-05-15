namespace NServiceBus.Azure
{
    using System.Diagnostics;

    public static class IsHostedIn
    {
        public static string HostProcessName = "NServiceBus.Hosting.Azure.HostProcess";

        public static bool ChildHostProcess()
        {
            var currentProcess = Process.GetCurrentProcess();
            return currentProcess.ProcessName == HostProcessName;
        }
    }
}