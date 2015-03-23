namespace NServiceBus.Unicast
{
    using System;
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
            Guard.AgainstNull(builder, "builder");
            Guard.AgainstNull(action, "action");
            foreach (var t in builder.BuildAll<T>())
            {
                action(t);
            }
        }
    }
}