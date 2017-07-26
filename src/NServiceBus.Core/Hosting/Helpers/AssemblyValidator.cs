namespace NServiceBus
{
    using System;
    using System.Reflection;

    class AssemblyValidator
    {
        public (bool shouldLoad, string reason) ValidateAssemblyFile(string assemblyPath)
        {
            try
            {
                var token = AssemblyName.GetAssemblyName(assemblyPath).GetPublicKeyToken();

                if (IsRuntimeAssembly(token))
                {
                    return (false, "File is a .NET runtime assembly.");
                }
            }
            catch (BadImageFormatException)
            {
                return (false, "File is not a .NET assembly.");
            }

            return (true, "File is a .NET assembly.");
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
    }
}
