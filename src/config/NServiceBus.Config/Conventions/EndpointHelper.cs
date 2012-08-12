namespace NServiceBus.Config.Conventions
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Web;

    /// <summary>
    /// The default name for a endpoint
    /// </summary>
    public static class EndpointHelper
    {
        private static Type entryType;
        private static bool intialized;

        /// <summary>
        /// Gets the name of this endpoint
        /// </summary>
        /// <returns>The name of the endpoint.</returns>
        public static string GetDefaultEndpointName()
        {
            Initialize();

            string endpointName = null;

            if (entryType != null)
            {
                endpointName = entryType.Namespace ?? entryType.Assembly.GetName().Name;
            }

            if (endpointName == null)
            {
                throw new InvalidOperationException(
                    "No endpoint name could be generated, please specify your own convention using Configure.DefineEndpointName()");
            }

            return endpointName;
        }

        /// <summary>
        /// Gets the version of the endpoint.
        /// </summary>
        /// <returns>The <see cref="Version"/> the endpoint.</returns>
        public static string GetEndpointVersion()
        {
            Initialize();

            if (entryType != null)
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(entryType.Assembly.Location);

                return new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart).ToString(3);
            }

            throw new InvalidOperationException(
                    "No version of the endpoint could not be retrieved using the default convention, please specify your own convention using Configure.DefineEndpointVersionRetriever()");
        }

        static void Initialize()
        {
            if (intialized)
                return;
            try
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null && entryAssembly.EntryPoint != null)
                {
                    entryType = entryAssembly.EntryPoint.ReflectedType;
                }

                var stackFrames = new StackTrace().GetFrames();
                StackFrame targetFrame = null;
                if (stackFrames != null)
                {
                    targetFrame =
                        stackFrames.FirstOrDefault(
                            f => typeof(HttpApplication).IsAssignableFrom(f.GetMethod().DeclaringType));
                }

                if (targetFrame != null)
                    entryType= targetFrame.GetMethod().ReflectedType;
            }
            finally
            {
                intialized = true;
            }
        }
    }
}