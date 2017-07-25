namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Particular.Licensing;

    class ConsoleLicensePrompt
    {
        public static License RequestLicenseFromConsole(License trialLicense)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(trialLicense.IsExtendedTrial ? "Your extended trial license has expired!" : "Your trial license has expired!");
            Console.ResetColor();

            License license = null;

            var options = new List<(string, Func<bool>)>();
            if (trialLicense.IsExtendedTrial)
            {
                options.Add(("to extend your trial further via our contact form", () =>
                {
                    OpenBrowser("https://particular.net/extend-your-trial-45");
                    return false;
                }));
            }
            else
            {
                options.Add(("to extend your trial license for FREE", () =>
                {
                    OpenBrowser("https://particular.net/extend-nservicebus-trial");
                    return false;
                }));
            }

            options.Add(("to purchase a license", () =>
            {
                OpenBrowser("https://particular.net/licensing");
                return false;
            }));
            options.Add(("to import a license", () =>
            {
                Console.WriteLine("Specify the path to your license file and press [Enter]:");
                var input = Console.ReadLine();

                if (File.Exists(input))
                {
                    Console.WriteLine("validating license file...");
                    var licenseText = File.ReadAllText(input);
                    Exception licenseVerifactionException;
                    if (LicenseVerifier.TryVerify(input, out licenseVerifactionException))
                    {
                        Console.WriteLine("Importing license...");
                        var providedLicense = LicenseDeserializer.Deserialize(licenseText);
                        if (!LicenseExpirationChecker.HasLicenseExpired(providedLicense))
                        {
                            license = providedLicense;
                            return true;
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

                return false;
            }));
            options.Add(("to continue without a license", () =>
            {
                Console.WriteLine();
                Console.WriteLine("Continuing without a license. NServiceBus will remain fully functional although continued use is in violation of our EULA.");
                Console.WriteLine();
                return true;
            }
            ));

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
                        var shouldContinue = options[optionIndex - 1].Item2();
                        if (shouldContinue)
                        {
                            return license;
                        }
                    }
                }
            }
        }

        // taken from: https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
        static void OpenBrowser(string url)
        {
#if NETCOREAPP2_0
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    Console.WriteLine($"Unable to open '{url}'. Please enter the url manually into your browser.");
                }
            }
#endif
        }
    }
}