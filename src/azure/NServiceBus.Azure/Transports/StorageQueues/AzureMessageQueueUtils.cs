namespace NServiceBus.Transports.StorageQueues
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Helper class 
    /// </summary>
    public class AzureMessageQueueUtils
    {

        public static string GetQueueName(Address address)
        {
            // The auto queue name generation uses namespaces which includes dots, 
            // yet dots are not supported in azure storage names
            // that's why we replace them here.

            var name = address.Queue.Replace('.', '-').ToLowerInvariant();

            if (name.Length > 63)
            {
                var nameGuid = DeterministicGuidBuilder(name).ToString();
                name = name.Substring(0, 63 - nameGuid.Length - 1) + "-" + nameGuid;
            }

            return name;
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            //use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                byte[] inputBytes = Encoding.Default.GetBytes(input);
                byte[] hashBytes = provider.ComputeHash(inputBytes);
                //generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}