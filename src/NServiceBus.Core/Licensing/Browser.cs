namespace NServiceBus
{
    using System;

#if NETCOREAPP2_0
    using System.Diagnostics;
    using System.Runtime.InteropServices;
#endif

    static class Browser
    {

        // taken from: https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
        public static void OpenBrowser(string url)
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
#if NET452
            throw new NotImplementedException();
#endif
        }
    }
}