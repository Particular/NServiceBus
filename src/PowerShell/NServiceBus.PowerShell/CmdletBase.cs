namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Security;
    using Setup.Windows;


    public abstract class CmdletBase : PSCmdlet
    {
        protected override void BeginProcessing()
        {
            if (!ProcessUtil.IsRunningWithElevatedPriviliges())
            {
                ThrowTerminatingError(new ErrorRecord(new SecurityException("You need to run this command with administrative rights."), "NotAuthorized", ErrorCategory.SecurityError, null));
            }
        }
    }
}