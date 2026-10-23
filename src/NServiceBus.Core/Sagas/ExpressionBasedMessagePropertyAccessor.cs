#nullable enable
namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FastExpressionCompiler;
using Sagas;


sealed class ExpressionBasedMessagePropertyAccessor<TMessage> : MessagePropertyAccessor<TMessage>
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "Falls back to Expression.Compile when dynamic code is not supported")]
    public ExpressionBasedMessagePropertyAccessor(Expression<Func<TMessage, object?>> propertyExpression) =>
        propertyAccessor = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ? propertyExpression.CompileFast() :
            propertyExpression.Compile();

    readonly Func<TMessage, object?> propertyAccessor;

    protected override object? AccessFrom(TMessage message) => propertyAccessor(message);
}