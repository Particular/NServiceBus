namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows;

    public class CmdletBase : PSCmdlet
    {
        protected void RequireElevatedPriviliges()
        {
            if(!ProcessUtil.IsRunningWithElevatedPriviliges())
                throw new Exception("This command requires elevated privliges, please re-run as administrator");
        }
    }
}