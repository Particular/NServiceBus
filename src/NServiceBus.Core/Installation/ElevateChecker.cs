namespace NServiceBus.Installation
{
    using System.Security.Principal;

    static class ElevateChecker
    {

        public static bool IsCurrentUserElevated()
        {
            using (var windowsIdentity = WindowsIdentity.GetCurrent())
            {
                if (windowsIdentity == null)
                {
                    return false;
                }
                var windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}