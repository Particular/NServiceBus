#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using IODirectory = System.IO.Directory;

static class Host
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Legacy ASP.NET hosting detection reflects over System.Web only when that assembly is already loaded.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Legacy ASP.NET hosting detection reflects over known public System.Web members only when that assembly is already loaded.")]
    public static string GetOutputDirectory()
    {
        Assembly? systemWebAssembly = null;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name != "System.Web")
            {
                continue;
            }

            systemWebAssembly = assembly;
            break;
        }

        var httpRuntime = systemWebAssembly?.GetType("System.Web.HttpRuntime");
        var appDomainAppId = httpRuntime?.GetProperty("AppDomainAppId", BindingFlags.Public | BindingFlags.Static);
        var result = appDomainAppId?.GetValue(null);

        return result == null ? AppDomain.CurrentDomain.BaseDirectory : DeriveAppDataPath(systemWebAssembly!);
    }

    static string DeriveAppDataPath(Assembly systemWebAssembly)
    {
        var appDataPath = TryMapPath(systemWebAssembly) ?? throw new Exception(GetMapPathError("Failed since MapPath returned null."));

        if (IODirectory.Exists(appDataPath))
        {
            return appDataPath;
        }

        throw new Exception(GetMapPathError($"Failed since path returned ({appDataPath}) does not exist. Ensure this directory is created and restart the endpoint."));
    }

    static readonly object[] parameters = ["~/App_Data/"];

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Legacy ASP.NET hosting detection reflects over System.Web only when that assembly is already loaded.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Legacy ASP.NET hosting detection reflects over known public System.Web members only when that assembly is already loaded.")]
    static string? TryMapPath(Assembly systemWebAssembly)
    {
        try
        {
            var hostingEnvironment = systemWebAssembly.GetType("System.Web.Hosting.HostingEnvironment");
            var mapPath = hostingEnvironment?.GetMethod("MapPath", BindingFlags.Static | BindingFlags.Public);
            var result = mapPath?.Invoke(null, parameters) as string;

            return result;
        }
        catch (Exception exception)
        {
            throw new Exception(GetMapPathError("Failed since MapPath threw an exception."), exception);
        }
    }

    static string GetMapPathError(string reason) => $"Detected running in a website and attempted to use HostingEnvironment.MapPath(\"~/App_Data/\") to derive the logging path. {reason}";
}