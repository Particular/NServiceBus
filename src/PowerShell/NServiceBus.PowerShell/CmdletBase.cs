namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Security;
    using Setup.Windows;


    public abstract class CmdletBase : PSCmdlet
    {
        protected override void BeginProcessing()
        {
            if (!ProcessUtil.IsRunningWithElevatedPrivileges())
            {
                var exception = new SecurityException("NServiceBus was unable to perform some infrastructure operations. You need to run this command with elevated privileges. If you are running this command from Visual Studio please close Visual Studio and re-open with elevated privileges. For more information see: http://particular.net/articles/preparing-your-machine-to-run-nservicebus");
                ThrowTerminatingError(new ErrorRecord(exception, "NotAuthorized", ErrorCategory.SecurityError, null));
            }
        }
    }
}