namespace NServiceBus.ObjectBuilder.Ninject.Internal
{
    using System;
    using global::Ninject.Activation;
    using global::Ninject.Syntax;

    public static class NinjectWhenExtensions
    {
        public static IBindingInNamedWithOrOnSyntax<T> WhenNoAnchestorNamed<T>(this IBindingWhenSyntax<T> syntax, string name)
        {
            return syntax.When(r => !IsAnyAnchestorNamed(r, name));
        }

        private static bool IsAnyAnchestorNamed(IRequest request, string name)
        {
            var parentContext = request.ParentContext;
            if (parentContext == null)
            {
                return false;
            }

            return
                string.Equals(parentContext.Binding.Metadata.Name, name, StringComparison.Ordinal) ||
                IsAnyAnchestorNamed(parentContext.Request, name);
        }
    }
}