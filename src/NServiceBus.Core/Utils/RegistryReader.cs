namespace NServiceBus.Utils
{
    using System;
    using Logging;
    using Microsoft.Win32;

    /// <summary>
    /// Wrapper to read registry keys.
    /// </summary>
    /// <typeparam name="T">The type of the key to retrieve</typeparam>
    public class RegistryReader<T>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RegistryReader<T>));

        /// <summary>
        /// Attempts to read the key from the registry.
        /// </summary>
        /// <param name="name">The name of the value to retrieve. This string is not case-sensitive.</param>
        /// <param name="defaultValue">The value to return if <paramref name="name"/> does not exist. </param>
        /// <returns>
        /// The value associated with <paramref name="name"/>, with any embedded environment variables left unexpanded, or <paramref name="defaultValue"/> if <paramref name="name"/> is not found.
        /// </returns>
        public static T Read(string name, T defaultValue = default(T))
        {
            try
            {              
                return ReadRegistryKeyValue(name, defaultValue);
            }
            catch (Exception ex)
            {
                Logger.Warn(string.Format(@"We couldn't read the registry to retrieve the {0}, from 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus'.", name), ex);
            }

            return defaultValue;
        }

        protected static T ReadRegistryKeyValue(string keyName, object defaultValue)
        {
            object value = null;

            if (Environment.Is64BitOperatingSystem)
            {
                value = ReadRegistry(RegistryView.Registry32, keyName, defaultValue) ?? ReadRegistry(RegistryView.Registry64, keyName, defaultValue);
            }
            else
            {
                value = ReadRegistry(RegistryView.Default, keyName, defaultValue);
            }

            return (T)value;
        }

        protected static object ReadRegistry(RegistryView view, string keyName, object defaultValue)
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).OpenSubKey(@"SOFTWARE\ParticularSoftware\ServiceBus"))
            {
                return key == null ? null : key.GetValue(keyName, defaultValue);
            }
        }
    }
}