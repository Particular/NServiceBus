namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;
    using LightInject;

    static class LightInjectRegistryExtensions
    {
        public static void Register(this IServiceRegistry registry, Type serviceType, Func<IServiceFactory, object> factoryDelegate, ILifetime lifetime, string serviceName)
        {
            var parameterExpression = Expression.Parameter(typeof(IServiceFactory), "factory");
            var invokeExpression = Expression.Invoke(Expression.Constant(factoryDelegate), parameterExpression);
            var castExpression = Expression.Convert(invokeExpression, serviceType);
            var lambdaExpression = Expression.Lambda(castExpression, parameterExpression);
            var lambda = lambdaExpression.Compile();

            var serviceRegistration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ServiceName = serviceName,
                FactoryExpression = lambda,
                Lifetime = lifetime
            };

            registry.Register(serviceRegistration);
        }
    }
}