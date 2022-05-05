namespace NServiceBus
{
    using System;
#if NETFRAMEWORK
    using System.Reflection;
#endif
#if NETCOREAPP
    using System.IO;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Security.Cryptography;
#endif

    class AssemblyValidator
    {
#if NETFRAMEWORK
        public void ValidateAssemblyFile(string assemblyPath, out bool shouldLoad, out string reason)
        {
            try
            {
                var token = AssemblyName.GetAssemblyName(assemblyPath).GetPublicKeyToken();

                if (IsRuntimeAssembly(token))
                {
                    shouldLoad = false;
                    reason = "File is a .NET runtime assembly.";
                    return;
                }
            }
            catch (BadImageFormatException)
            {
                shouldLoad = false;
                reason = "File is not a .NET assembly.";
                return;
            }

            shouldLoad = true;
            reason = "File is a .NET assembly.";
        }
#endif

#if NETCOREAPP
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

        static byte[] GetPublicKeyToken(byte[] publicKey)
        {
            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(publicKey);
                var publicKeyToken = new byte[8];

                for (var i = 0; i < 8; i++)
                {
                    publicKeyToken[i] = hash[hash.Length - (i + 1)];
                }

                return publicKeyToken;
            }
        }
#endif

        public static bool IsRuntimeAssembly(byte[] publicKeyToken)
        {
            var tokenString = BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToLowerInvariant();

            return tokenString switch
            {
                // Microsoft tokens
                "b77a5c561934e089" or "7cec85d7bea7798e" or "b03f5f7f11d50a3a" or "31bf3856ad364e35" or "cc7b13ffcd2ddd51" or "adb9793829ddae60" or "7e34167dcc6d6d8c" or "23ec7fc2d6eaa4a5" => true,
                _ => false,
            };
        }
    }
}
