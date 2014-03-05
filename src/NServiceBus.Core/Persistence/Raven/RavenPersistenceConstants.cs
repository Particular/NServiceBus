namespace NServiceBus.Persistence.Raven
{
    using System;
    using Utils;

    public static class RavenPersistenceConstants
    {
        public const int DefaultPort = 8080;

        private static readonly int registryPort = DefaultPort;

        static RavenPersistenceConstants()
        {
            registryPort = RegistryReader<int>.Read("RavenPort", DefaultPort);
        }

        public static string DefaultUrl
        {
            get
            {
                var masterNode = Configure.Instance.GetMasterNode();

                if (string.IsNullOrEmpty(masterNode))
                    masterNode = "localhost";

                return string.Format("http://{0}:{1}", masterNode, registryPort);
            }
        }
        
        public static Guid DefaultResourceManagerId
        {
            get
            {
                return DeterministicGuid.Create(Address.Local, "-", Configure.DefineEndpointVersionRetriever());
            }
        }
    }
}