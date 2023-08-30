namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Security.Cryptography;

    static class AssemblyValidator
    {
        public static void ValidateAssemblyFile(string assemblyPath, out bool shouldLoad, out string reason)
        {
            using var stream = File.OpenRead(assemblyPath);
            using var file = new PEReader(stream);
            var hasMetadata = false;

            try
            {
                hasMetadata = file.HasMetadata;
            }
            catch (BadImageFormatException) { }

            if (!hasMetadata)
            {
                shouldLoad = false;
                reason = "File is not a .NET assembly.";
                return;
            }

            var reader = file.GetMetadataReader();
            var assemblyDefinition = reader.GetAssemblyDefinition();

            if (!assemblyDefinition.PublicKey.IsNil)
            {
                var publicKey = reader.GetBlobBytes(assemblyDefinition.PublicKey);
                var publicKeyToken = GetPublicKeyToken(publicKey);

                if (IsRuntimeAssembly(publicKeyToken))
                {
                    shouldLoad = false;
                    reason = "File is a .NET runtime assembly.";
                    return;
                }
            }

            shouldLoad = true;
            reason = "File is a .NET assembly.";
        }

        public static bool IsRuntimeAssembly(AssemblyName assemblyName)
        {
            var tokenString = Convert.ToHexString(assemblyName.GetPublicKeyToken() ?? Array.Empty<byte>());
            return IsRuntimeAssembly(tokenString);
        }

        static string GetPublicKeyToken(byte[] publicKey)
        {
            using var sha1 = SHA1.Create();
            Span<byte> publicKeyToken = stackalloc byte[20];
            // returns false when the destination doesn't have enough space
            _ = sha1.TryComputeHash(publicKey, publicKeyToken, out _);
            Span<byte> lastEightBytes = publicKeyToken.Slice(publicKeyToken.Length - 8, 8);
            lastEightBytes.Reverse();
            return Convert.ToHexString(lastEightBytes);
        }

        static bool IsRuntimeAssembly(string tokenString) =>
            tokenString switch
            {
                // Microsoft tokens
                "B77A5C561934E089" or "7CEC85D7BEA7798E" or "B03F5F7F11D50A3A" or "31BF3856AD364E35" or "CC7B13FFCD2DDD51" or "ADB9793829DDAE60" or "7E34167DCC6D6D8C" or "23EC7FC2D6EAA4A5" => true,
                _ => false,
            };
    }
}
