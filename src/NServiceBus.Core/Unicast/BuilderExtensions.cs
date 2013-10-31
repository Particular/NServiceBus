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
        public static void ForEach<T>(this IBuilder builder, Action<T> action)
        {
            var list = builder.BuildAll<T>().ToList();

            list.ForEach(action);
        }
    }
}