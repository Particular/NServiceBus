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
        /// <param name="assemblyExpression"><see cref="Configure.IsMatch"/></param>
        /// <seealso cref="Configure.IsMatch"/>
        /// <returns></returns>
        IExcludesBuilder And(string assemblyExpression);
    }
}