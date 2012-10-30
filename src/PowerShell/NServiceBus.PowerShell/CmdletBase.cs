namespace NServiceBus.PowerShell
{
    using System;
    using System.ComponentModel;
    using System.Management.Automation;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Principal;
    using Setup.Windows;


    public abstract class CmdletBase : PSCmdlet
    {
        protected abstract void Process();

        protected override void ProcessRecord()
        {
            if (ProcessUtil.IsRunningWithElevatedPriviliges())
            {
                Process();
            }
            else
            {
                throw new SecurityException("You need to run this command with administrative rights.");
            }
        }
    }
}