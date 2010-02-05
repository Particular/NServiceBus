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

            var sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));

            return sBuilder.ToString();
        }
    }
}
