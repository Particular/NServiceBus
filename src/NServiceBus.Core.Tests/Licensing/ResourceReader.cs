namespace NServiceBus.Core.Tests.Licensing
{
    using System.IO;
    using System.Reflection;

    public static class ResourceReader
    {
        public static string ReadResourceAsString(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + path))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}