﻿#if NETSTANDARD
namespace NServiceBus
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    static class Browser
    {
        public static bool TryOpen(string url)
        {
            using (var process = new Process())
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = url;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    process.StartInfo.FileName = "xdg-open";
                    process.StartInfo.Arguments = url;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    process.StartInfo.FileName = "open";
                    process.StartInfo.Arguments = url;
                }

                try
                {
                    process.Start();
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#else
namespace NServiceBus
{
    using System.Diagnostics;

    static class Browser
    {
        public static bool TryOpen(string url)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = url;

                try
                {
                    process.Start();
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
