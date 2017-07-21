namespace NServiceBus
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    static class DeterministicGuid
    {
        public static Guid Create(params object[] data)
        {
            // use MD5 hash to get a 16-byte hash of the string
#pragma warning disable PC001
            using (var provider = new MD5CryptoServiceProvider())
#pragma warning restore PC001
            {
                var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}