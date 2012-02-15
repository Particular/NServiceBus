using System.Reflection;
using NServiceBus.Config;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Licensing
{
    public class Bootstrapper : INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            var validated = Configure.Instance.HasValidLicense();

            if (!validated)
            {
                var transport = Configure.Instance.Builder.Build<TransactionalTransport>();
                var numWorkerThreadsInfo = typeof (TransactionalTransport).GetField("numberOfWorkerThreads",
                                                         BindingFlags.Instance | BindingFlags.NonPublic);

                //intentionally don't check for null so that this will blow up if there are changes
                numWorkerThreadsInfo.SetValue(transport, 1);
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<LicenseManager>(new LicenseManager());
        }
    }
}