namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Supporting the fluent interface in <seealso cref="AllAssemblies"/>
    /// </summary>
    public interface IExcludesBuilder : IEnumerable<Assembly>
    {
        /// <summary>
        /// Indicate that the given assembly expression should also be excluded.
        /// You can call this method multiple times.
        /// </summary>
        IExcludesBuilder And(string assemblyExpression);
    }
}