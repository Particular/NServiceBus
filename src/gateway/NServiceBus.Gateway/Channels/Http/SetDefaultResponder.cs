namespace NServiceBus.Gateway.Channels.Http
{
    using NServiceBus.Config;

    public class SetDefaultResponder:IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<IHttpResponder>())
                Configure.Instance.Configurer.ConfigureComponent<DefaultResponder>(DependencyLifecycle.InstancePerCall);   
        }
    }
}