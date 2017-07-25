namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Particular.Licensing;

    class ConsoleLicensePrompt
    {
        public static License RequestLicenseFromConsole()
        {
            Console.Clear();
            Console.WriteLine(@"
#################################################
        Thank you for using NServiceBus
  ---------------------------------------------
          Your trial license expired!
  ---------------------------------------------
    Press:
    [1] to extend your trial license for FREE
    [2] to purchase a license
    [3] to import a license
    [4] to continue without a license.
#################################################
");
            while (true)
            {
                var input = Console.ReadKey();
                switch (input.KeyChar)
                {
                    case '1':
                        OpenBrowser("https://particular.net/extend-nservicebus-trial");
                        break;
                    case '2':
                        OpenBrowser("https://particular.net/licensing");
                        break;
                    case '3':
                        throw new NotImplementedException();
                    case '4':
                        Console.WriteLine();
                        Console.WriteLine("Continuing without a license. NServiceBus will remain fully functional although continued use is in violation of our EULA.");
                        Console.WriteLine();
                        return null;
                    default:
                        break;
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