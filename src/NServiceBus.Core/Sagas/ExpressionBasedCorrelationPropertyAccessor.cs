#nullable enable
namespace NServiceBus;

using System;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Sagas;

sealed class ExpressionBasedCorrelationPropertyAccessor<TSagaData>(PropertyInfo propertyInfo) : CorrelationPropertyAccessor
    where TSagaData : IContainSagaData
{
    readonly Func<TSagaData, object?> propertyGetter = CreateGetter(propertyInfo);
    readonly Action<TSagaData, object?> propertySetter = CreateSetter(propertyInfo);

    public override object? AccessFrom(IContainSagaData sagaData) => propertyGetter((TSagaData)sagaData);

    public override void WriteTo(IContainSagaData sagaData, object value) => propertySetter((TSagaData)sagaData, value);

    static Func<TSagaData, object?> CreateGetter(PropertyInfo propertyInfo)
    {
        var instance = Expression.Parameter(typeof(TSagaData), "instance");
        var property = Expression.Property(instance, propertyInfo);
        var convert = Expression.Convert(property, typeof(object));
        var lambda = Expression.Lambda<Func<TSagaData, object?>>(convert, instance);

        return System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported
            ? lambda.CompileFast()
            : lambda.Compile();
    }

    static Action<TSagaData, object?> CreateSetter(PropertyInfo propertyInfo)
    {
        var instance = Expression.Parameter(typeof(TSagaData), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var property = Expression.Property(instance, propertyInfo);
        var convert = Expression.Convert(value, propertyInfo.PropertyType);
        var assign = Expression.Assign(property, convert);
        var lambda = Expression.Lambda<Action<TSagaData, object?>>(assign, instance, value);

        return System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported
            ? lambda.CompileFast()
            : lambda.Compile();
    }
}