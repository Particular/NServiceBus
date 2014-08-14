namespace NServiceBus.Config.Conventions
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Web;

    class EndpointHelper
    {
        StackTrace stackTraceToExamine;
        Type entryType;
        bool initialized;

        public EndpointHelper(StackTrace stackTraceToExamine)
        {
            this.stackTraceToExamine = stackTraceToExamine;
            if (Debugger.IsAttached)
            {
                EnsureThisCodeIsNotBeingCalledFromIConfigureThisEndpoint();
            }
        }

        /// <summary>
        /// Gets the name of this endpoint
        /// </summary>
        /// <returns>The name of the endpoint.</returns>
        public string GetDefaultEndpointName()
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
        public string GetEndpointVersion()
        {
            Initialize();

            if (entryType != null)
            {
                return FileVersionRetriever.GetFileVersion(entryType);
            }

            throw new InvalidOperationException(
                    "No version of the endpoint could not be retrieved using the default convention, please specify your own convention using Configure.DefineEndpointVersionRetriever()");
        }


        public void EnsureThisCodeIsNotBeingCalledFromIConfigureThisEndpoint()
        {
            var stackFrames = stackTraceToExamine.GetFrames();
            if (stackFrames == null)
            {
                return;
            }

            var targetFrame =
                stackFrames.FirstOrDefault(
                    f =>
                    {
                        var methodBase = f.GetMethod();
                        return typeof(IConfigureThisEndpoint).IsAssignableFrom(methodBase.DeclaringType) && methodBase.Name == "Customize";
                    });

            if (targetFrame != null)
            {
                throw new InvalidOperationException("Do not call Configure.With from IConfigureThisEndpoint.Customize. Instead use the \"builder\" argument passed to the Customize method. To customize other options not available from the builder, implement the INeedInitialization interface. Configure.With() is only useful in self hosting scenarios.");
            }
        }


        void Initialize()
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
                    entryType = targetFrame.GetMethod().ReflectedType;
                    return;
                }

                if (stackTraceToExamine != null)
                {
                    stackFrames = stackTraceToExamine.GetFrames();
                    if (stackFrames != null)
                    {
                        targetFrame =
                            stackFrames.FirstOrDefault(
                                f =>
                                {
                                    var declaringType = f.GetMethod().DeclaringType;
                                    return declaringType != typeof(Configure) && declaringType != typeof(ConfigurationBuilder);
                                });
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
