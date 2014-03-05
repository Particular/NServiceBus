namespace NServiceBus.Impersonation.Windows
{
    /// <summary>
    /// Configures windows impersonation
    /// </summary>
    [ObsoleteEx(
        Message = "The impersonation feature has been removed due to confusion of it being a security feature." +
                  "Once you stop using this feature the Thread.CurrentPrincipal will no longer be set to a fake principal containing the username. However you can still get access to that information using the message headers.",
        Replacement = "message.GetHeader(Headers.WindowsIdentityName)",
        RemoveInVersion = "5.0",
        TreatAsErrorFromVersion = "4.3")]
    public class ConfigureWindowsImpersonation : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            //default to Windows impersonation if no other impersonation is configured
            if (Configure.Instance.Configurer.HasComponent<ExtractIncomingPrincipal>())
            {
                return;
            }

            Configure.Instance.Configurer.ConfigureComponent<WindowsImpersonator>(DependencyLifecycle.SingleInstance);
        }

    }
}