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
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "CompileFast not used when IsDynamicCodeSupported is true.")]
    readonly Func<TMessage, object?> propertyAccessor = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ? propertyExpression.CompileFast() :
        // Fall back to Expression.Compile or reflection-based getter
        propertyExpression.Compile();

    protected override object? AccessFrom(TMessage message) => propertyAccessor(message);
}