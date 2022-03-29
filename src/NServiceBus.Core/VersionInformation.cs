namespace NServiceBus
{
    using System.Reflection;

    static class VersionInformation
    {
        public static string MajorMinorPatch { get; }

        static VersionInformation()
        {
            MajorMinorPatch = "0.0.0";

            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>();

            foreach (var attribute in attributes)
            {
                if (attribute.Key == "MajorMinorPatch")
                {
                    MajorMinorPatch = attribute.Value;
                }
            }
        }
    }
}
