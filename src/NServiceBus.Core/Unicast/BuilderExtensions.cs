namespace NServiceBus.Unicast
{
    using System;
    using System.Linq;
    using ObjectBuilder;

    /// <summary>
    /// Extension methods for IBuilder
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Applies the action on the instances of T
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        public static void ForEach<T>(this IBuilder builder, Action<T> action)
        {
            var objs = builder.BuildAll<T>().ToList();

            objs.ForEach(action);
        }
    }
}