namespace Particular.Licensing
{
    using System;
    using System.Linq;
    using System.Reflection;

    static class ReleaseDateReader
    {
        public static DateTime GetReleaseDate()
        {
            var attribute = (dynamic)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(false)
                .FirstOrDefault(x => x.GetType().Name == "ReleaseDateAttribute");

            if (attribute == null)
            {
                throw new Exception("No ReleaseDateAttribute could be found in assembly, please make sure GitVersion is enabled");
            }

            return UniversalDateParser.Parse((string)attribute.OriginalDate);
        }
    }
}