namespace NServiceBus
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    static class DeterministicGuid
    {
        public static Guid Create(string data1, string data2) => Create($"{data1}{data2}");

        public static Guid Create(string data)
        {
            // use MD5 hash to get a 16-byte hash of the string
            var inputBytes = Encoding.Default.GetBytes(data);

            Span<byte> hashBytes = stackalloc byte[16];

            _ = MD5.HashData(inputBytes, hashBytes);

            // generate a guid from the hash:
            return new Guid(hashBytes);
        }
    }
}