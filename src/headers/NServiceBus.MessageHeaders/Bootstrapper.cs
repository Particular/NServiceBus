using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.MessageHeaders
{
    class Bootstrapper : INeedInitialization
    {
        void INeedInitialization.Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MessageHeaderManager>(DependencyLifecycle.SingleInstance);

            Configure.ConfigurationComplete +=
                (s, args) =>
                    {
                        var mgr = Configure.Instance.Builder.Build<MessageHeaderManager>();

                        ExtensionMethods.GetHeaderAction = (msg, key) => mgr.GetHeader(msg, key);
                        ExtensionMethods.SetHeaderAction = (msg, key, val) => mgr.SetHeader(msg, key, val);
                        ExtensionMethods.GetStaticOutgoingHeadersAction = () => mgr.GetStaticOutgoingHeaders();
                    };
        }
    }
}
