#nullable enable

namespace NServiceBus
{
    using System;
    using System.Buffers;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Text;

    static class DeterministicGuid
    {
        public static Guid Create(string data1, string data2) => Create($"{data1}{data2}");

        [SkipLocalsInit]
        public static Guid Create(string data)
        {
            const int MaxStackLimit = 256;
            var encoding = Encoding.UTF8;
            var maxByteCount = encoding.GetMaxByteCount(data.Length);

            byte[]? sharedBuffer = null;
            var stringBufferSpan = maxByteCount <= MaxStackLimit ?
                stackalloc byte[MaxStackLimit] :
                sharedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

            try
            {
                var numberOfBytesWritten = encoding.GetBytes(data, stringBufferSpan);
                Span<byte> hashBytes = stackalloc byte[16];

                _ = MD5.HashData(stringBufferSpan[..numberOfBytesWritten], hashBytes);

                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
            finally
            {
                if (sharedBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(sharedBuffer, clearArray: true);
                }
            }
        }
    }
}