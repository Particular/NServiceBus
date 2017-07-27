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
            var options = new List<(string optionText, Func<(string result, License license)> optionAction)>();

            if (trialLicense.IsExtendedTrial)
            {
                options.Add(("extend your trial further via our contact form", () =>
                {
                    var result = Browser.Open("https://particular.net/extend-your-trial-45");
                    return (result, null);
                }
                ));
            }
            else
            {
                options.Add(("extend your trial license for FREE", () =>
                {
                    var result = Browser.Open("https://particular.net/extend-nservicebus-trial");
                    return (result, null);
                }
                ));
            }

            options.Add(("purchase a license", () =>
            {
                var result = Browser.Open("https://particular.net/licensing");
                return (result, null);
            }
            ));

            options.Add(("import a license", ImportLicense));

            options.Add(("continue without a license", () =>
            {
                var result = Environment.NewLine + "Continuing without a license. NServiceBus will remain fully functional although continued use is in violation of our EULA." + Environment.NewLine;
                return (result, trialLicense);
            }
            ));

            while (true)
            {
                ListOptions(trialLicense, options);

                Console.Write("Select an option: ");
                var input = Console.ReadKey();
                Console.WriteLine();

                if (int.TryParse(input.KeyChar.ToString(), out var optionIndex))
                {
                    if (optionIndex > 0 && optionIndex <= options.Count)
                    {
                        var (result, license) = options[optionIndex - 1].optionAction();

                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            Console.WriteLine(result);
                            Console.Write("[press any key to continue]");
                            Console.ReadKey();
                        }

                        if (license != null)
                        {
                            return license;
                        }
                    }
                }
            }
        }

        static void ListOptions(License trialLicense, List<(string optionText, Func<(string result, License license)> optionAction)> options)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(trialLicense.IsExtendedTrial ? "Your extended trial license has expired!" : "Your trial license has expired!");
            Console.ResetColor();

            Console.WriteLine("Press:");

            for (var i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {options[i].optionText}");
            }
        }

        static (string result, License license) ImportLicense()
        {
            Console.Clear();

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
                    return ($"No file found at '{input}'", null);
                }

                Console.WriteLine("Validating license file...");
                var licenseText = File.ReadAllText(input);

                if (!LicenseVerifier.TryVerify(licenseText, out var licenseVerifactionException))
                {
                    return ("Specified file does not contain a valid license: " + licenseVerifactionException.Message, null);
                }

                Console.WriteLine("Importing license...");
                var providedLicense = LicenseDeserializer.Deserialize(licenseText);

                if (LicenseExpirationChecker.HasLicenseExpired(providedLicense))
                {
                    return ("Imported license has expired. Please provide a valid license.", null);
                }

                // Store licenses locally in the License subfolder which will be probed as a license source
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License"));
                }

                File.Copy(input, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License", "license.xml"), true);

                return ("License successfully imported.", providedLicense);
            }
            catch (Exception e)
            {
                return ("Failed to import license: " + e, null);
            }
        }
    }
}