namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Security;
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