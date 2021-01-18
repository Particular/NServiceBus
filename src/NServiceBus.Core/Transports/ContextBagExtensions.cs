namespace NServiceBus
{
    using System.Collections.Generic;
    using Extensibility;
    using Transport;

    static class ContextBagExtensions
    {
        public static void AddOperationProperties(this ContextBag context, OperationProperties properties)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(properties), properties);

            var contextProperties = new OperationProperties(properties);
            context.Set(contextProperties);
        }

        public static OperationProperties AsOperationProperties(this Dictionary<string, string> properties)
        {
            return new OperationProperties(properties);
        }
    }
}