namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Particular.Licensing;

    class ConsoleLicensePrompt
    {
        public static License RequestLicenseFromConsole(License trialLicense)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(trialLicense.IsExtendedTrial ? "Your extended trial license has expired!" : "Your trial license has expired!");
            Console.ResetColor();

            var options = new List<(string optionText, Func<License> optionAction)>();

            if (trialLicense.IsExtendedTrial)
            {
                options.Add(("extend your trial further via our contact form", () =>
                {
                    Browser.Open("https://particular.net/extend-your-trial-45");
                    return null;
                }));
            }
            else
            {
                options.Add(("extend your trial license for FREE", () =>
                {
                    Browser.Open("https://particular.net/extend-nservicebus-trial");
                    return null;
                }));
            }

            options.Add(("purchase a license", () =>
            {
                Browser.Open("https://particular.net/licensing");
                return null;
            }));

            options.Add(("import a license", ImportLicense));

            options.Add(("continue without a license", () =>
            {
                Console.WriteLine();
                Console.WriteLine("Continuing without a license. NServiceBus will remain fully functional although continued use is in violation of our EULA.");
                Console.WriteLine();
                return trialLicense;
            }));

            ListOptions(options);

            while (true)
            {
                Console.Write("Select an option: ");
                var input = Console.ReadKey();
                Console.WriteLine();

                if (int.TryParse(input.KeyChar.ToString(), out var optionIndex))
                {
                    if (optionIndex > 0 && optionIndex <= options.Count)
                    {
                        var license = options[optionIndex - 1].optionAction();

                        if (license != null)
                        {
                            return license;
                        }
                    }
                }
            }
        }

        static void ListOptions(List<ValueTuple<string, Func<License>>> options)
        {
            Console.WriteLine("Press:");

            for (var i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {options[i].Item1}");
            }
        }

        static License ImportLicense()
        {
            Console.WriteLine("Specify the path to your license file and press [Enter]:");
            var input = Console.ReadLine();

            try
            {
                if (!Path.IsPathRooted(input))
                {
                    input = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, input);
                }

                if (!File.Exists(input))
                {
                    Console.WriteLine($"No file found at '{input}'");
                    return null;
                }

                Console.WriteLine("Validating license file...");
                var licenseText = File.ReadAllText(input);

                if (!LicenseVerifier.TryVerify(licenseText, out var licenseVerifactionException))
                {
                    Console.WriteLine("Specified file does not contain a valid license: " + licenseVerifactionException.Message);
                    return null;
                }

                Console.WriteLine("Importing license...");
                var providedLicense = LicenseDeserializer.Deserialize(licenseText);

                if (LicenseExpirationChecker.HasLicenseExpired(providedLicense))
                {
                    Console.WriteLine("Imported license has expired. Please provide a valid license.");
                    return null;
                }

                // Store licenses locally in the License subfolder which will be probed as a license source
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License"));
                }

                File.Copy(input, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License", "license.xml"), true);
                Console.WriteLine("License successfully imported.");

                return providedLicense;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to import license: " + e);
                return null;
            }
        }
    }
}