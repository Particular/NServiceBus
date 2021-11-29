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
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
                var hashBytes = md5.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}