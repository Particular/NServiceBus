namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Security.Cryptography;

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
