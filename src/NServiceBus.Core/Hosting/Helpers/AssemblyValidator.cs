﻿namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Security.Cryptography;

    class AssemblyValidator
    {
        public void ValidateAssemblyFile(string assemblyPath, out bool shouldLoad, out string reason)
        {
            using (var stream = File.OpenRead(assemblyPath))
            using (var file = new PEReader(stream))
            {
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
        }

        public static bool IsRuntimeAssembly(byte[] publicKeyToken)
        {
            var tokenString = BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToLowerInvariant();

            //Compare token to known Microsoft tokens

            if (tokenString == "b77a5c561934e089")
            {
                return true;
            }

            if (tokenString == "7cec85d7bea7798e")
            {
                return true;
            }

            if (tokenString == "b03f5f7f11d50a3a")
            {
                return true;
            }

            if (tokenString == "31bf3856ad364e35")
            {
                return true;
            }

            if (tokenString == "cc7b13ffcd2ddd51")
            {
                return true;
            }

            return false;
        }

        byte[] GetPublicKeyToken(byte[] publicKey)
        {
            var hash = provider.ComputeHash(publicKey);
            var publicKeyToken = new byte[8];

            for (var i = 0; i < 8; i++)
            {
                publicKeyToken[i] = hash[hash.Length - (i + 1)];
            }

            return publicKeyToken;
        }
#pragma warning disable PC001
        SHA1CryptoServiceProvider provider = new SHA1CryptoServiceProvider();
#pragma warning restore PC001
    }
}
