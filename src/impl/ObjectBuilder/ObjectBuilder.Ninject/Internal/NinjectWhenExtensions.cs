namespace NServiceBus.ObjectBuilder.Ninject.Internal
{
    using System;
    using global::Ninject.Activation;
    using global::Ninject.Syntax;

    internal static class NinjectWhenExtensions
    {
        public static IBindingInNamedWithOrOnSyntax<T> WhenNoAncestorNamed<T>(this IBindingWhenSyntax<T> syntax, string name)
        {
            return syntax.When(r => !IsAnyAncestorNamed(r, name));
        }

        private static bool IsAnyAncestorNamed(IRequest request, string name)
        {
            var parentContext = request.ParentContext;
            if (parentContext == null)
            {
                return false;
            }

            return
                string.Equals(parentContext.Binding.Metadata.Name, name, StringComparison.Ordinal) ||
                IsAnyAncestorNamed(parentContext.Request, name);
        }
    }
}