using System;
using System.Text;
using System.Security.Cryptography;

namespace NServiceBus.Gateway
{
    public class Hasher
    {
        public const string HeaderKey = "NServiceBus.Header.Gateway.Hash";

        public static string Hash(byte[] buffer)
        {
            MD5 hasher = MD5.Create();
            byte[] data = hasher.ComputeHash(buffer);

            return Convert.ToBase64String(data);
        }
    }
}
