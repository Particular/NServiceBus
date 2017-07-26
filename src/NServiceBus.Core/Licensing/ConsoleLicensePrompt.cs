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

            var options = new List<(string, Func<License>)>();
            if (trialLicense.IsExtendedTrial)
            {
                options.Add(("to extend your trial further via our contact form", () =>
                {
                    Browser.OpenBrowser("https://particular.net/extend-your-trial-45");
                    return null;
                }));
            }
            else
            {
                options.Add(("to extend your trial license for FREE", () =>
                {
                    Browser.OpenBrowser("https://particular.net/extend-nservicebus-trial");
                    return null;
                }));
            }

            options.Add(("to purchase a license", () =>
            {
                Browser.OpenBrowser("https://particular.net/licensing");
                return null;
            }));
            options.Add(("to import a license", () =>
            {
                Console.WriteLine("Specify the path to your license file and press [Enter]:");
                var input = Console.ReadLine();
                if (!Path.IsPathRooted(input))
                {
                    input = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, input);
                }

                if (File.Exists(input))
                {
                    Console.WriteLine("Validating license file...");
                    var licenseText = File.ReadAllText(input);
                    Exception licenseVerifactionException;
                    if (LicenseVerifier.TryVerify(licenseText, out licenseVerifactionException))
                    {
                        Console.WriteLine("Importing license...");
                        var providedLicense = LicenseDeserializer.Deserialize(licenseText);
                        if (!LicenseExpirationChecker.HasLicenseExpired(providedLicense))
                        {

                            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License")))
                            {
                                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License"));
                            }

                            File.Copy(input, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License", "license.xml"), true);
                            return providedLicense;
                        }

                        Console.WriteLine("Imported license has expired. Please provide a valid license.");
                    }
                    else
                    {
                        Console.WriteLine("Specified file does not contain a valid license: " + licenseVerifactionException.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"No file found at {input}");
                }

                return null;
            }));
            options.Add(("to continue without a license", () =>
            {
                Console.WriteLine();
                Console.WriteLine("Continuing without a license. NServiceBus will remain fully functional although continued use is in violation of our EULA.");
                Console.WriteLine();
                return trialLicense;
            }));

            Console.WriteLine("Press:");
            for (var i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {options[i].Item1}");
            }

            while (true)
            {
                var input = Console.ReadKey();
                Console.WriteLine();
                int optionIndex;
                if (int.TryParse(input.KeyChar.ToString(), out optionIndex))
                {
                    if (optionIndex > 0 && optionIndex <= options.Count)
                    {
                        var license = options[optionIndex - 1].Item2();
                        if (license != null)
                        {
                            return license;
                        }
                    }
                }
            }
        }
    }
}