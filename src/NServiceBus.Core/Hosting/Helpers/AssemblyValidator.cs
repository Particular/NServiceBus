namespace NServiceBus
{
    using System;
#if NET452
    using System.Reflection;
#endif
#if NETSTANDARD2_0
    using System.IO;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Security.Cryptography;
#endif

    class AssemblyValidator
    {
#if NET452
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

#if NETSTANDARD2_0
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

            if (tokenString == "adb9793829ddae60")
            {
                return true;
            }

            return false;
        }
    }
}
