#nullable enable
namespace NServiceBus;

using System;
using System.Linq.Expressions;
using FastExpressionCompiler;

sealed class ExpressionBasedMessagePropertyAccessor<TMessage>(Expression<Func<TMessage, object?>> propertyExpression)
    : MessagePropertyAccessor<TMessage>
{
    readonly Func<TMessage, object?> propertyAccessor = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ? propertyExpression.CompileFast() :
        // Fall back to Expression.Compile or reflection-based getter
        propertyExpression.Compile();

    protected override object? AccessFrom(TMessage message) => propertyAccessor(message);
}