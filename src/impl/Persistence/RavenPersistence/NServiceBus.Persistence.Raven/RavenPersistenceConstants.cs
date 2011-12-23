using System;

namespace NServiceBus.Persistence.Raven
{
    using System.Diagnostics;
    using System.Security.Cryptography;
    using System.Text;

    public static class RavenPersistenceConstants
    {
        public const string DefaultDataDirectory = @".\NServiceBusData";
        public const string DefaultUrl = "http://localhost:8080";
        public static Guid DefaultResourceManagerId
        {
            get
            {
                var resourceManagerId = Configure.EndpointName + "@" + Environment.MachineName;

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