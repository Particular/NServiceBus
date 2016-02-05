namespace NServiceBus.Unicast
{
    using System;
    using ObjectBuilder;

    /// <summary>
    /// Extension methods for <see cref="IChildBuilder"/>.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Applies the action on the instances of <typeparamref name="T"/>.
        /// </summary>
        public static void ForEach<T>(this IChildBuilder builder, Action<T> action)
        {
            Guard.AgainstNull(nameof(builder), builder);
            Guard.AgainstNull(nameof(action), action);
            foreach (var t in builder.BuildAll<T>())
            {
                action(t);
            }
        }
    }
}