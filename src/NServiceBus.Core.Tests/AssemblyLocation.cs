using System;
using System.IO;

public static class AssemblyLocation
{
    public static string CurrentDirectory
    {
        get
        {
            return Path.GetDirectoryName(CurrentAssemblyPath);
        }
    }
    public static string CurrentAssemblyPath
    {
        get
        {
            //Use codebase because location fails for unit tests.
            var assembly = typeof(AssemblyLocation).Assembly;
            var uri = new UriBuilder(assembly.CodeBase);
            return Uri.UnescapeDataString(uri.Path).Replace('/','\\');
        }
    }
}