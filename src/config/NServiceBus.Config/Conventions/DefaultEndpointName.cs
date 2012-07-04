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
    public static class DefaultEndpointName
    {
        /// <summary>
        /// Gets the name of this endpoint
        /// </summary>
        /// <returns></returns>
        public static string Get()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null && entryAssembly.EntryPoint != null)
            {
                return entryAssembly.EntryPoint.ReflectedType.Namespace ??
                       entryAssembly.EntryPoint.ReflectedType.Assembly.GetName().Name;
            }

            var stackFrames = new StackTrace().GetFrames();
            StackFrame targetFrame = null;
            if (stackFrames != null)
            {
                targetFrame =
                    stackFrames.FirstOrDefault(
                        f => typeof (HttpApplication).IsAssignableFrom(f.GetMethod().DeclaringType));
            }

            if (targetFrame != null)
                return targetFrame.GetMethod().ReflectedType.Namespace ??
                       targetFrame.GetMethod().ReflectedType.Assembly.GetName().Name;

            throw new InvalidOperationException(
                "No endpoint name could be generated, please specify your own convention using Configure.DefineEndpointName(...)");
        }
    }
}