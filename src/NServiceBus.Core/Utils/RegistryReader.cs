namespace NServiceBus
{
    using System;
    using Logging;
    using Microsoft.Win32;

    static class RegistryReader
    {
        public static string Read(string name, string defaultValue = null)
        {
            try
            {
                return ReadRegistryKeyValue(name, defaultValue);
            }
            catch (Exception ex)
            {
                Logger.Warn($@"Could not read the registry to retrieve the {name}, from 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus'.", ex);
            }

            return defaultValue;
        }

        static string ReadRegistryKeyValue(string keyName, string defaultValue)
        {
            if (Environment.Is64BitOperatingSystem)
            {
                return ReadRegistry(RegistryView.Registry32, keyName, defaultValue) ?? ReadRegistry(RegistryView.Registry64, keyName, defaultValue);
            }
            return ReadRegistry(RegistryView.Default, keyName, defaultValue);
        }

        static string ReadRegistry(RegistryView view, string keyName, string defaultValue)
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).OpenSubKey(@"SOFTWARE\ParticularSoftware\ServiceBus"))
            {
                if (key == null)
                {
                    return defaultValue;
                }
                return (string) key.GetValue(keyName, defaultValue);
            }
        }

        static ILog Logger = LogManager.GetLogger(typeof(RegistryReader));
    }
}