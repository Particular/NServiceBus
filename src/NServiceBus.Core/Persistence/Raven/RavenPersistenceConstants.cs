namespace NServiceBus.Persistence.Raven
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
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
                var resourceManagerId = Address.Local + "-" + Configure.DefineEndpointVersionRetriever() ;
                
                return DeterministicGuidBuilder(resourceManagerId);
            }
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(input);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}