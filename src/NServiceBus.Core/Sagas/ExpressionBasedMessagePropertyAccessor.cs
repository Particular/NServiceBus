#nullable enable
namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FastExpressionCompiler;
using Sagas;


sealed class ExpressionBasedMessagePropertyAccessor<TMessage>(Expression<Func<TMessage, object?>> propertyExpression)
    : MessagePropertyAccessor<TMessage>
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "Falls back to Expression.Compile when dynamic code is not supported")]
    readonly Func<TMessage, object?> propertyAccessor = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ? propertyExpression.CompileFast() :
        propertyExpression.Compile();

    protected override object? AccessFrom(TMessage message) => propertyAccessor(message);
}