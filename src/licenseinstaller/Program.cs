namespace LicenseInstaller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Win32;
    using NDesk.Options;

    class Program
    {
        static string licensePath;
        static bool useHKCU;

        [STAThread]
        static int Main(string[] args)
        {
            if (!TryParseOptions(args))
            {
                return 0;
            }

            if (!File.Exists(licensePath))
            {
                Console.Out.WriteLine("License file '{0}' could not be installed.", licensePath);
                return 1;
            }

            var selectedLicenseText = ReadAllTextWithoutLocking(licensePath);

            if (Environment.Is64BitOperatingSystem)
            {
                if (!TryToWriteToRegistry(selectedLicenseText, RegistryView.Registry32))
                {
                    return 1;
                }

                if (!TryToWriteToRegistry(selectedLicenseText, RegistryView.Registry64))
                {
                    return 1;
                }
            }
            else
            {
                if (!TryToWriteToRegistry(selectedLicenseText, RegistryView.Default))
                {
                    return 1;
                }
            }
            
            Console.Out.WriteLine("License file installed.");

            return 0;
        }

        static bool TryToWriteToRegistry(string selectedLicenseText, RegistryView view)
        {
            var rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);

            if (useHKCU)
            {
                rootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
            }

            using (var registryKey = rootKey.CreateSubKey(@"SOFTWARE\ParticularSoftware\NServiceBus"))
            {
                if (registryKey == null)
                {
                    Console.Out.WriteLine("License file not installed.");
                    return false;
                }
                registryKey.SetValue("License", selectedLicenseText, RegistryValueKind.String);
            }
            return true;
        }

        static bool TryParseOptions(IEnumerable<string> args)
        {
            OptionSet optionSet = null;
            Func<bool> action = () => true;

            optionSet = new OptionSet
                {
                    {
                        "current-user|c",
                        @"Installs license in HKEY_CURRENT_USER\SOFTWARE\ParticularSoftware\NServiceBus, by default if not specified the license is installed in HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\NServiceBus"
                        , s => action = () =>
                            {
                                useHKCU = true;
                                return true;
                            }
                    },
                    {
                        "help|h|?", "Help about the command line options", key => action = () =>
                            {
                                PrintUsage(optionSet);
                                return false;
                            }
                    },
                };

            try
            {
                var unparsedArgs = optionSet.Parse(args);

                if (unparsedArgs.Count > 0)
                {
                    licensePath = unparsedArgs[0];
                }
                else
                {
                    PrintUsage(optionSet);
                    return false;
                }
                
                return action();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                PrintUsage(optionSet);
            }

            return false;
        }

        static void PrintUsage(OptionSet optionSet)
        {
            Console.WriteLine(
                @"
NServiceBus license installer
--------------------------------------------------------
Copyright 2010 - {0} - NServiceBus. All rights reserved
--------------------------------------------------------

Usage: LicenseInstaller [options] license_path
Options:", DateTime.Now.Year);

            optionSet.WriteOptionDescriptions(Console.Out);

            Console.Out.WriteLine();
        }

        static string ReadAllTextWithoutLocking(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var textReader = new StreamReader(fileStream))
            {
                return textReader.ReadToEnd();
            }
        }

    }
}
