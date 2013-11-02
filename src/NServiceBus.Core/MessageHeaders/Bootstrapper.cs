namespace NServiceBus.MessageHeaders
{
    using Config;
    using INeedInitialization = INeedInitialization;

    class Bootstrapper : INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        void INeedInitialization.Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MessageHeaderManager>(DependencyLifecycle.SingleInstance);
        }

        public void Run()
        {
            ExtensionMethods.GetHeaderAction = (msg, key) => Manager.GetHeader(msg, key);
            ExtensionMethods.SetHeaderAction = (msg, key, val) => Manager.SetHeader(msg, key, val);
            ExtensionMethods.GetStaticOutgoingHeadersAction = () => Manager.GetStaticOutgoingHeaders();            
        }

        public MessageHeaderManager Manager { get; set; }
    }
}
