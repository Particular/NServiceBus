using System;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.MessageHeaders
{
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
