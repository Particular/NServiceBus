namespace NServiceBus.Utils;

using System;
using System.Security.Cryptography;
using System.Text;

static class DeterministicGuid
{
    public static Guid Create(params object[] data)
    {
        var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
        var hashBytes = MD5.HashData(inputBytes);
        // generate a guid from the hash:
        return new Guid(hashBytes);
    }
}