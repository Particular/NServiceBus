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
        private static bool initialized;

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
                return FileVersionRetriever.GetFileVersion(entryType);
            }

            throw new InvalidOperationException(
                    "No version of the endpoint could not be retrieved using the default convention, please specify your own convention using Configure.DefineEndpointVersionRetriever()");
        }

        /// <summary>
        /// If set this will be used to figure out what to name the endpoint and select the version.
        /// </summary>
        public static StackTrace StackTraceToExamine { get; set; }

        static void Initialize()
        {
            if (initialized)
                return;
            try
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null && entryAssembly.EntryPoint != null)
                {
                    entryType = entryAssembly.EntryPoint.ReflectedType;
                    return;
                }

                StackFrame targetFrame = null;

                var stackFrames = new StackTrace().GetFrames();
                if (stackFrames != null)
                {
                    targetFrame =
                        stackFrames.FirstOrDefault(
                            f => typeof(HttpApplication).IsAssignableFrom(f.GetMethod().DeclaringType));
                }

                if (targetFrame != null)
                {
                    entryType= targetFrame.GetMethod().ReflectedType;
                    return;
                }

                if (StackTraceToExamine != null)
                {
                    stackFrames = StackTraceToExamine.GetFrames();
                    if (stackFrames != null)
                    {
                        targetFrame =
                            stackFrames.FirstOrDefault(
                                f => f.GetMethod().DeclaringType != typeof(Configure));

                      
                    }
                }

                if (targetFrame == null)
                    targetFrame = stackFrames.FirstOrDefault(
                       f => f.GetMethod().DeclaringType.Assembly != typeof(Configure).Assembly);

                if (targetFrame != null)
                {
                    entryType = targetFrame.GetMethod().ReflectedType;
                }
            }
            finally
            {
                initialized = true;
            }
        }
    }
}