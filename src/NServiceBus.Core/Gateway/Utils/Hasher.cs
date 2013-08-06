namespace NServiceBus.Gateway.Utils
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    public class Hasher
    {
        public static string Hash(Stream stream)
        {
            var position = stream.Position;
            var hash = MD5.Create().ComputeHash(stream);

            stream.Position = position;

            return Convert.ToBase64String(hash);
        }
    }
}