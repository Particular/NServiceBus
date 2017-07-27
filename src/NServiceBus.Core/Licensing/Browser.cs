#if NETCOREAPP2_0
namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    static class Browser
    {
        public static string Open(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var info = new ProcessStartInfo(url)
                    {
#pragma warning disable PC001
                        UseShellExecute = true
#pragma warning restore PC001
                    };

                    Process.Start(info);
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
                    throw new Exception("Unknown OSPlatform");
                }
            }
            catch
            {
                return $"Unable to open '{url}'. Please enter the url manually into your browser.";
            }

            return null;
        }
    }
}
#else
namespace NServiceBus
{
    using System.Diagnostics;

    static class Browser
    {
        public static string Open(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                return $"Unable to open '{url}'. Please enter the url manually into your browser.";
            }

            return null;
        }
    }
}
#endif
