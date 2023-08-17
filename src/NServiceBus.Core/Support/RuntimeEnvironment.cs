namespace NServiceBus.Support
{
    using System;

    /// <summary>
    /// Abstracts the runtime environment.
    /// </summary>
    public static partial class RuntimeEnvironment
    {
        static RuntimeEnvironment()
        {
            MachineName = Environment.MachineName;
        }

        /// <summary>
        /// Returns the machine name where this endpoint is currently running.
        /// </summary>
        public static string MachineName { get; private set; }


        internal static void SetMachineName(string machineName)
        {
            MachineName = machineName;
        }
    }
}