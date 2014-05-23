namespace NServiceBus.Utils
{
    using System;
    using Logging;
    using Microsoft.Win32;

    static class RegistryReader<T>
    {
        static ILog Logger = LogManager.GetLogger(typeof(RegistryReader<T>));

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

        static T ReadRegistryKeyValue(string keyName, object defaultValue)
        {
            object value;

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

        static object ReadRegistry(RegistryView view, string keyName, object defaultValue)
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).OpenSubKey(@"SOFTWARE\ParticularSoftware\ServiceBus"))
            {
                return key == null ? defaultValue : key.GetValue(keyName, defaultValue);
            }
        }
    }
}