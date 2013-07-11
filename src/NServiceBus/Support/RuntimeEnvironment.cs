namespace NServiceBus.Support
{
    using System;

    /// <summary>
    /// Abstracts the runtime environment
    /// </summary>
    public static class RuntimeEnvironment
    {
        static RuntimeEnvironment()
        {
            MachineNameAction = () => Environment.MachineName;
        }

        /// <summary>
        /// Returns the machine name where this endpoint is currently running
        /// </summary>
        public static string MachineName 
        {
            get { return MachineNameAction(); }
        }


        /// <summary>
        /// Get the machine name, allows for overrides
        /// </summary>
        public static Func<string> MachineNameAction { get; set; }
    }
}