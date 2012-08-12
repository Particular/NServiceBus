using System;

namespace NServiceBus.Persistence.Raven
{
    using System.Diagnostics;
    using System.Security.Cryptography;
    using System.Text;

    public static class RavenPersistenceConstants
    {
        public const int DefaultPort = 8080;
        public static string DefaultUrl
        {
            get
            {
                var masterNode = Configure.Instance.GetMasterNode();

                if (string.IsNullOrEmpty(masterNode))
                    masterNode = "localhost";

                return string.Format("http://{0}:{1}", masterNode, DefaultPort);
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

        [DebuggerNonUserCode]
        static Guid DeterministicGuidBuilder(string input)
        {
            //use MD5 hash to get a 16-byte hash of the string
            var provider = new MD5CryptoServiceProvider();
            byte[] inputBytes = Encoding.Default.GetBytes(input);
            byte[] hashBytes = provider.ComputeHash(inputBytes);
            //generate a guid from the hash:
            return new Guid(hashBytes);
        }
    }
}