namespace NServiceBus
{
    using System.Security.Principal;

    static class ElevateChecker
    {
        public static bool IsCurrentUserElevated()
        {
            using (var windowsIdentity = WindowsIdentity.GetCurrent())
            {
                var windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}