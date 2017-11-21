namespace NServiceBus
{
    using System;
    using System.Reflection;

    class AssemblyValidator
    {
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
