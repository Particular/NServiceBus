namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Supporting the fluent interface in <seealso cref="AllAssemblies"/>
    /// </summary>
    public interface IIncludesBuilder : IEnumerable<Assembly>
    {
        /// <summary>
        /// Indicate that assemblies matching the given expression should also be included.
        /// You can call this method multiple times.
        /// </summary>
        /// <param name="assemblyExpression"><see cref="Configure.IsMatch"/></param>
        /// <seealso cref="Configure.IsMatch"/>
        /// <returns></returns>
        IIncludesBuilder And(string assemblyExpression);

        /// <summary>
        /// Indicate that assemblies matching the given expression should be excluded.
        /// Use the 'And' method to indicate other assemblies to be skipped.
        /// </summary>
        /// <param name="assemblyExpression"><see cref="Configure.IsMatch"/></param>
        /// <seealso cref="Configure.IsMatch"/>
        /// <returns></returns>
        IExcludesBuilder Except(string assemblyExpression);
    }
}