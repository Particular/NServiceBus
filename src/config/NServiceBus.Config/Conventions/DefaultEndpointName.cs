namespace NServiceBus.Config.Conventions
{
    using System;
    using System.Diagnostics;
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
            var trace = new StackTrace();
            StackFrame targetFrame = null;
            var stackFrames = trace.GetFrames();
            if (stackFrames == null)
                return "";

            foreach (var f in stackFrames)
            {
                if (typeof(HttpApplication).IsAssignableFrom(f.GetMethod().DeclaringType))
                {
                    targetFrame = f;
                    break;
                }
                var mi = f.GetMethod() as MethodInfo;
                if (mi != null && mi.IsStatic && mi.ReturnType == typeof(void) && mi.Name == "Main" && mi.DeclaringType.Name == "Program")
                {
                    targetFrame = f;
                    break;
                }
            }

            if (targetFrame != null)
                return targetFrame.GetMethod().ReflectedType.Namespace;

            throw new InvalidOperationException(
                "No endpoint name could be generated, please specify your own convention using Configure.DefineEndpointName(...)");
        }
    }
}