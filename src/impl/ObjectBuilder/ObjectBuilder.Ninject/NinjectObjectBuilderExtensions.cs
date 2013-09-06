namespace NServiceBus.ObjectBuilder.Ninject
{
    using global::Ninject.Extensions.NamedScope;
    using global::Ninject.Syntax;
    using Internal;

    public static class NinjectObjectBuilderExtensions
    {
        const string ScopeName = "NinjectObjectBuilder";

        /// <summary>
        /// Defines a conditional binding which is applied when the requested service is in an unit of work.
        /// </summary>
        /// <typeparam name="T">The requested service type.</typeparam>
        /// <param name="syntax">The syntax</param>
        /// <returns>The binding</returns>
        public static IBindingInNamedWithOrOnSyntax<T> WhenInUnitOfWork<T>(this IBindingWhenSyntax<T> syntax)
        {
            return syntax.WhenAnyAnchestorNamed(ScopeName);
        }

        /// <summary>
        /// Defines a conditional binding which is applied when the requested service is NOT in an unit of work.
        /// </summary>
        /// <typeparam name="T">The requested service type.</typeparam>
        /// <param name="syntax">The syntax</param>
        /// <returns>The binding</returns>
        public static IBindingInNamedWithOrOnSyntax<T> WhenNotInUnitOfWork<T>(this IBindingWhenSyntax<T> syntax)
        {
            return syntax.WhenNoAncestorNamed(ScopeName);
        }

        /// <summary>
        /// Defines the unit of work scope on the requested service.
        /// </summary>
        /// <typeparam name="T">The requested service type.</typeparam>
        /// <param name="syntax">The syntax.</param>
        /// <returns>The binding.</returns>
        public static IBindingNamedWithOrOnSyntax<T> InUnitOfWorkScope<T>(this IBindingInSyntax<T> syntax)
        {
            return syntax.InNamedScope(ScopeName);
        }

        internal static void DefinesNinjectObjectBuilderScope<T>(this IBindingWhenInNamedWithOrOnSyntax<T> syntax)
        {
            syntax.Named(ScopeName).DefinesNamedScope(ScopeName);
        }
    }
}