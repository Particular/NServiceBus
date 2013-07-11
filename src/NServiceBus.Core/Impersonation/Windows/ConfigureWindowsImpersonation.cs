namespace NServiceBus.Impersonation.Windows
{
    /// <summary>
    /// Configures windows impersonation
    /// </summary>
    public class ConfigureWindowsImpersonation : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            //default to Windows impersonation if no other impersonation is configured
            if (Configure.Instance.Configurer.HasComponent<ExtractIncomingPrincipal>())
                return;

            Configure.Instance.Configurer.ConfigureComponent<WindowsIdentityEnricher>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<WindowsImpersonator>(DependencyLifecycle.SingleInstance);
        }

    }
}