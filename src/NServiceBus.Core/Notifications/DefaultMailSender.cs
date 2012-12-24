namespace NServiceBus.Notifications
{
    class DefaultMailSender : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<ISendMail>())
            {
                Configure.Instance.Configurer.ConfigureComponent<SmtpClientSender>(DependencyLifecycle.SingleInstance);
            }
        }
    }
}