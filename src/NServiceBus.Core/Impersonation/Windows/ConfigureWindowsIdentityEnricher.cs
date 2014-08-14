namespace NServiceBus.Impersonation.Windows
{
    class ConfigureWindowsIdentityEnricher : INeedInitialization
    {
        public void Customize(ConfigurationBuilder builder)
        {
            builder.RegisterComponents(r=> r.ConfigureComponent<WindowsIdentityEnricher>(DependencyLifecycle.SingleInstance));
        }
    }
}