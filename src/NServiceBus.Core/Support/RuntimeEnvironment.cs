namespace NServiceBus.Support
{
    using System;

    /// <summary>
    /// Abstracts the runtime environment.
    /// </summary>
    public static class RuntimeEnvironment
    {
        static RuntimeEnvironment()
        {
            var machineName = Environment.MachineName;

            MachineNameAction = () => machineName;
        }

        /// <summary>
        /// Returns the machine name where this endpoint is currently running.
        /// </summary>
        public static string MachineName => MachineNameAction();


        /// <summary>
        /// Get the machine name, allows for overrides.
        /// </summary>
        public static Func<string> MachineNameAction { get; set; }
    }
}