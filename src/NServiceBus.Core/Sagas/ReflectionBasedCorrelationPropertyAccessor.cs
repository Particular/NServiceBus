#nullable enable
namespace NServiceBus;

using System.Reflection;

sealed class ReflectionBasedCorrelationPropertyAccessor(PropertyInfo propertyInfo) : CorrelationPropertyAccessor
{
    public override void WriteTo(IContainSagaData sagaData, object value) => propertyInfo.SetValue(sagaData, value);
    public override object? AccessFrom(IContainSagaData sagaData) => propertyInfo.GetValue(sagaData);
}