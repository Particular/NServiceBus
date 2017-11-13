/*
The MIT License (MIT)

Copyright (c) 2016 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included AddOrUpdateServiceFactory
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

/*
 v1.10.1 from https://github.com/dadhi/FastExpressionCompiler/commit/a05df8d48963decd84f44637c12d80df739690de
 Public types made internal
*/

// ReSharper disable CoVariantArrayConversion

namespace FastExpressionCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>Compiles expression to delegate ~20 times faster than Expression.Compile.
    /// Partial to extend with your things when used as source file.</summary>
    // ReSharper disable once PartialTypeWithSinglePart
    static partial class ExpressionCompiler
    {
        #region Obsolete APIs

        /// <summary>Obsolete: replaced by CompileFast extension method</summary>
        public static Func<T> Compile<T>(Expression<Func<T>> lambdaExpr) =>
            lambdaExpr.CompileFast<Func<T>>();

        /// <summary>Obsolete: replaced by CompileFast extension method</summary>
        public static TDelegate Compile<TDelegate>(LambdaExpression lambdaExpr)
            where TDelegate : class =>
            TryCompile<TDelegate>(lambdaExpr) ?? (TDelegate)(object)lambdaExpr.Compile();

        #endregion

        #region Expression.CompileFast overloads for Delegate, Funcs, and Actions

        /// <summary>Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static TDelegate CompileFast<TDelegate>(this LambdaExpression lambdaExpr, bool ifFastFailedReturnNull = false)
            where TDelegate : class =>
            TryCompile<TDelegate>(lambdaExpr) ?? (ifFastFailedReturnNull ? null : (TDelegate)(object)lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Delegate CompileFast(this LambdaExpression lambdaExpr, bool ifFastFailedReturnNull = false) =>
            lambdaExpr.CompileFast<Delegate>(ifFastFailedReturnNull);

        /// <summary>Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static TDelegate CompileFast<TDelegate>(this Expression<TDelegate> lambdaExpr, bool ifFastFailedReturnNull = false)
            where TDelegate : class =>
            TryCompile<TDelegate>(lambdaExpr) ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<R> CompileFast<R>(this Expression<Func<R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<R>>(lambdaExpr.Body, lambdaExpr.Parameters, Tools.Empty<Type>(), typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, R> CompileFast<T1, R>(this Expression<Func<T1, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, R> CompileFast<T1, T2, R>(this Expression<Func<T1, T2, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, R> CompileFast<T1, T2, T3, R>(
            this Expression<Func<T1, T2, T3, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, R> CompileFast<T1, T2, T3, T4, R>(
            this Expression<Func<T1, T2, T3, T4, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, T5, R> CompileFast<T1, T2, T3, T4, T5, R>(
            this Expression<Func<T1, T2, T3, T4, T5, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, T5, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, T5, T6, R> CompileFast<T1, T2, T3, T4, T5, T6, R>(
            this Expression<Func<T1, T2, T3, T4, T5, T6, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, T5, T6, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action CompileFast(this Expression<Action> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action>(lambdaExpr.Body, lambdaExpr.Parameters, Tools.Empty<Type>(), typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1> CompileFast<T1>(this Expression<Action<T1>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2> CompileFast<T1, T2>(this Expression<Action<T1, T2>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3> CompileFast<T1, T2, T3>(this Expression<Action<T1, T2, T3>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4> CompileFast<T1, T2, T3, T4>(
            this Expression<Action<T1, T2, T3, T4>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4, T5> CompileFast<T1, T2, T3, T4, T5>(
            this Expression<Action<T1, T2, T3, T4, T5>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4, T5>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4, T5, T6> CompileFast<T1, T2, T3, T4, T5, T6>(
            this Expression<Action<T1, T2, T3, T4, T5, T6>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4, T5, T6>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.Compile());

        #endregion

        #region ExpressionInfo.CompileFast overloads for Delegate, Funcs, and Actions

        /// <summary>Compiles lambda expression info to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static TDelegate CompileFast<TDelegate>(this LambdaExpressionInfo lambdaExpr, bool ifFastFailedReturnNull = false)
            where TDelegate : class =>
            TryCompile<TDelegate>(lambdaExpr) ?? (ifFastFailedReturnNull ? null : (TDelegate)(object)lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Delegate CompileFast(this LambdaExpressionInfo lambdaExpr, bool ifFastFailedReturnNull = false) =>
            lambdaExpr.CompileFast<Delegate>(ifFastFailedReturnNull);

        /// <summary>Compiles lambda expression info to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static TDelegate CompileFast<TDelegate>(this ExpressionInfo<TDelegate> lambdaExpr, bool ifFastFailedReturnNull = false)
            where TDelegate : class =>
            TryCompile(lambdaExpr) ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<R> CompileFast<R>(this ExpressionInfo<Func<R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<R>>(lambdaExpr.Body, lambdaExpr.Parameters, Tools.Empty<Type>(), typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, R> CompileFast<T1, R>(this ExpressionInfo<Func<T1, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, R> CompileFast<T1, T2, R>(this ExpressionInfo<Func<T1, T2, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, R> CompileFast<T1, T2, T3, R>(
            this ExpressionInfo<Func<T1, T2, T3, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, R> CompileFast<T1, T2, T3, T4, R>(
            this ExpressionInfo<Func<T1, T2, T3, T4, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, T5, R> CompileFast<T1, T2, T3, T4, T5, R>(
            this ExpressionInfo<Func<T1, T2, T3, T4, T5, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, T5, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, T5, T6, R> CompileFast<T1, T2, T3, T4, T5, T6, R>(
            this ExpressionInfo<Func<T1, T2, T3, T4, T5, T6, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, T5, T6, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action CompileFast(this ExpressionInfo<Action> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action>(lambdaExpr.Body, lambdaExpr.Parameters, Tools.Empty<Type>(), typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1> CompileFast<T1>(this ExpressionInfo<Action<T1>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2> CompileFast<T1, T2>(this ExpressionInfo<Action<T1, T2>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3> CompileFast<T1, T2, T3>(this ExpressionInfo<Action<T1, T2, T3>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4> CompileFast<T1, T2, T3, T4>(
            this ExpressionInfo<Action<T1, T2, T3, T4>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4, T5> CompileFast<T1, T2, T3, T4, T5>(
            this ExpressionInfo<Action<T1, T2, T3, T4, T5>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4, T5>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.ToLambdaExpression().Compile());

        /// <summary>Compiles lambda expression info to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4, T5, T6> CompileFast<T1, T2, T3, T4, T5, T6>(
            this ExpressionInfo<Action<T1, T2, T3, T4, T5, T6>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4, T5, T6>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, typeof(void))
            ?? (ifFastFailedReturnNull? null : lambdaExpr.ToLambdaExpression().Compile());

        #endregion

        /// <summary>Tries to compile lambda expression to <typeparamref name="TDelegate"/>.</summary>
        public static TDelegate TryCompile<TDelegate>(this LambdaExpression lambdaExpr)
            where TDelegate : class =>
            TryCompile<TDelegate>(lambdaExpr.Body, lambdaExpr.Parameters,
                Tools.GetParamExprTypes(lambdaExpr.Parameters), lambdaExpr.ReturnType);

        /// <summary>Tries to compile lambda expression to <typeparamref name="TDelegate"/> 
        /// with the provided closure object and constant expressions (or lack there of) -
        /// Constant expression should be the in order of Fields in closure object!
        /// Note 1: Use it on your own risk - FEC won't verify the expression is compile-able with passed closure, it is up to you!
        /// Note 2: The expression with NESTED LAMBDA IS NOT SUPPORTED!</summary>
        public static TDelegate TryCompileWithPreCreatedClosure<TDelegate>(this LambdaExpression lambdaExpr,
            object closure, params ConstantExpression[] closureConstantsExprs)
            where TDelegate : class
        {
            var closureInfo = new ClosureInfo(true, closure, closureConstantsExprs);
            var bodyExpr = lambdaExpr.Body;
            var returnType = bodyExpr.Type;
            var paramExprs = lambdaExpr.Parameters;
            return (TDelegate)TryCompile(ref closureInfo, typeof(TDelegate), Tools.GetParamExprTypes(paramExprs),
                returnType, bodyExpr, bodyExpr.NodeType, returnType, paramExprs.AsArray());
        }

        /// <summary>Tries to compile expression to "static" delegate, skipping the step of collecting the closure object.</summary>
        public static TDelegate TryCompileWithoutClosure<TDelegate>(this LambdaExpression lambdaExpr)
            where TDelegate : class => lambdaExpr.TryCompileWithPreCreatedClosure<TDelegate>(null, null);

        /// <summary>Tries to compile lambda expression INFO to <typeparamref name="TDelegate"/> 
        /// with the provided closure object and constant expressions (or lack there of) -
        /// Constant expression should be the in order of Fields in closure object!
        /// Note 1: Use it on your own risk - FEC won't verify the expression is compile-able with passed closure, it is up to you!
        /// Note 2: The expression with NESTED LAMBDA IS NOT SUPPORTED!</summary>
        public static TDelegate TryCompileWithPreCreatedClosure<TDelegate>(this LambdaExpressionInfo lambdaExpr,
            object closure, params object[] closureConstantsExprs)
            where TDelegate : class
        {
            var closureInfo = new ClosureInfo(true, closure, closureConstantsExprs);
            var bodyExpr = lambdaExpr.Body;
            var returnType = bodyExpr.GetResultType();
            var paramExprs = lambdaExpr.Parameters;
            return (TDelegate)TryCompile(ref closureInfo, typeof(TDelegate), Tools.GetParamExprTypes(paramExprs),
                returnType, bodyExpr, bodyExpr.GetNodeType(), returnType, paramExprs.AsArray());
        }

        /// <summary>Tries to compile expression info to "static" delegate, skipping the step of collecting the closure object.</summary>
        public static TDelegate TryCompileWithoutClosure<TDelegate>(this LambdaExpressionInfo lambdaExpr)
            where TDelegate : class => lambdaExpr.TryCompileWithPreCreatedClosure<TDelegate>(null, null);

        /// <summary>Compiles expression to delegate by emitting the IL. 
        /// If sub-expressions are not supported by emitter, then the method returns null.
        /// The usage should be calling the method, if result is null then calling the Expression.Compile.</summary>
        public static TDelegate TryCompile<TDelegate>(
            Expression bodyExpr, IList<ParameterExpression> paramExprs, Type[] paramTypes, Type returnType)
            where TDelegate : class
        {
            var ignored = new ClosureInfo(false);
            return (TDelegate)TryCompile(ref ignored, typeof(TDelegate),
                paramTypes, returnType, bodyExpr, bodyExpr.NodeType, bodyExpr.Type, paramExprs.AsArray());
        }

        /// <summary>Tries to compile lambda expression info.</summary>
        public static TDelegate TryCompile<TDelegate>(this LambdaExpressionInfo lambdaExpr)
            where TDelegate : class =>
            TryCompile<TDelegate>(lambdaExpr.Body, lambdaExpr.Parameters,
                Tools.GetParamExprTypes(lambdaExpr.Parameters), lambdaExpr.Body.GetResultType());

        /// <summary>Tries to compile lambda expression info.</summary>
        public static Delegate TryCompile(this LambdaExpressionInfo lambdaExpr) =>
            TryCompile<Delegate>(lambdaExpr);

        /// <summary>Tries to compile lambda expression info.</summary>
        public static TDelegate TryCompile<TDelegate>(this ExpressionInfo<TDelegate> lambdaExpr)
            where TDelegate : class =>
            TryCompile<TDelegate>((LambdaExpressionInfo)lambdaExpr);

        // todo: Not used, candidate for removal
        /// <summary>Compiles expression to delegate by emitting the IL. 
        /// If sub-expressions are not supported by emitter, then the method returns null.
        /// The usage should be calling the method, if result is null then calling the Expression.Compile.</summary>
        public static TDelegate TryCompile<TDelegate>(
            ExpressionInfo bodyExpr, IList<ParameterExpression> paramExprs, Type[] paramTypes, Type returnType)
            where TDelegate : class
        {
            var ignored = new ClosureInfo(false);
            return (TDelegate)TryCompile(ref ignored, typeof(TDelegate),
                paramTypes, returnType, bodyExpr, bodyExpr.NodeType, returnType, paramExprs.AsArray());
        }

        // todo: Not used, candidate for removal
        /// <summary>Obsolete</summary>
        public static TDelegate TryCompile<TDelegate>(
            ExpressionInfo bodyExpr, IList<ParameterExpressionInfo> paramExprs, Type[] paramTypes, Type returnType)
            where TDelegate : class
        {
            var ignored = new ClosureInfo(false);
            return (TDelegate)TryCompile(ref ignored, typeof(TDelegate),
                paramTypes, returnType, bodyExpr, bodyExpr.NodeType, returnType, paramExprs.AsArray());
        }

        /// <summary>Compiles expression to delegate by emitting the IL. 
        /// If sub-expressions are not supported by emitter, then the method returns null.
        /// The usage should be calling the method, if result is null then calling the Expression.Compile.</summary>
        public static TDelegate TryCompile<TDelegate>(
            object bodyExpr, object[] paramExprs, Type[] paramTypes, Type returnType) where TDelegate : class
        {
            var ignored = new ClosureInfo(false);
            return (TDelegate)TryCompile(ref ignored, typeof(TDelegate),
                paramTypes, returnType, bodyExpr, bodyExpr.GetNodeType(), returnType, paramExprs);
        }

        private static object TryCompile(ref ClosureInfo closureInfo,
            Type delegateType, Type[] paramTypes, Type returnType,
            object exprObj, ExpressionType exprNodeType, Type exprType, object[] paramExprs,
            bool isNestedLambda = false)
        {
            object closure;
            if (closureInfo.IsClosureConstructed)
                closure = closureInfo.Closure;
            else if (TryCollectBoundConstants(ref closureInfo, exprObj, exprNodeType, paramExprs))
                closure = closureInfo.ConstructClosureTypeAndObject(constructTypeOnly: isNestedLambda);
            else
                return null;

            var closureType = closureInfo.ClosureType;
            var methodParamTypes = closureType == null ? paramTypes : GetClosureAndParamTypes(paramTypes, closureType);

            var method = new DynamicMethod(string.Empty, returnType, methodParamTypes,
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = method.GetILGenerator();
            if (!EmittingVisitor.TryEmit(exprObj, exprNodeType, exprType, paramExprs, il, 
                ref closureInfo, ExpressionType.Default))
                return null;

            if (returnType == typeof(void) && exprType != typeof(void))
                il.Emit(OpCodes.Pop); // discard the return value on stack (#71)

            il.Emit(OpCodes.Ret);

            // include closure as the first parameter, BUT don't bound to it. It will be bound later in EmitNestedLambda.
            if (isNestedLambda)
                delegateType = Tools.GetFuncOrActionType(methodParamTypes, returnType);
            // create a specific delegate if user requested delegate is untyped, otherwise CreateMethod will fail
            else if (delegateType == typeof(Delegate))
                delegateType = Tools.GetFuncOrActionType(paramTypes, returnType);

            return method.CreateDelegate(delegateType, closure);
        }

        private static Type[] GetClosureAndParamTypes(Type[] paramTypes, Type closureType)
        {
            var paramCount = paramTypes.Length;
            if (paramCount == 0)
                return new[] { closureType };

            if (paramCount == 1)
                return new[] { closureType, paramTypes[0] };

            var closureAndParamTypes = new Type[paramCount + 1];
            closureAndParamTypes[0] = closureType;
            Array.Copy(paramTypes, 0, closureAndParamTypes, 1, paramCount);
            return closureAndParamTypes;
        }

        private sealed class BlockInfo
        {
            public static readonly BlockInfo Empty = new BlockInfo();

            public bool IsEmpty => Parent == null;
            public readonly BlockInfo Parent;
            public readonly object ResultExpr;
            public readonly object[] VarExprs;
            public readonly LocalBuilder[] LocalVars;

            private BlockInfo() { }

            internal BlockInfo(BlockInfo parent,
                object resultExpr, object[] varExprs, LocalBuilder[] localVars)
            {
                Parent = parent;
                ResultExpr = resultExpr;
                VarExprs = varExprs;
                LocalVars = localVars;
            }
        }

        // todo: Consolidate together with IL, ParamObjects and parent ExperssionType into single context passed with TryEmit methods
        // Track the info required to build a closure object + some context information not directly related to closure.
        private struct ClosureInfo
        {
            public bool IsClosureConstructed;

            // Constructed closure object.
            public readonly object Closure;

            // Type of constructed closure, may be available even without closure object (in case of nested lambda)
            public Type ClosureType;

            public bool HasClosure => ClosureType != null;

            // Constant expressions to find an index (by reference) of constant expression from compiled expression.
            public object[] Constants;

            // Parameters not passed through lambda parameter list But used inside lambda body.
            // The top expression should not! contain non passed parameters. 
            public object[] NonPassedParameters;

            // All nested lambdas recursively nested in expression
            public NestedLambdaInfo[] NestedLambdas;

            public int ClosedItemCount => Constants.Length + NonPassedParameters.Length + NestedLambdas.Length;

            // FieldInfos are needed to load field of closure object on stack in emitter.
            // It is also an indicator that we use typed Closure object and not an array.
            public FieldInfo[] ClosureFields;

            // Helper to decide whether we are inside the block or not
            public BlockInfo CurrentBlock;

            // Populates info directly with provided closure object and constants.
            public ClosureInfo(bool isConstructed, object closure = null, object[] closureConstantExpressions = null)
            {
                IsClosureConstructed = isConstructed;

                NonPassedParameters = Tools.Empty<object>();
                NestedLambdas = Tools.Empty<NestedLambdaInfo>();
                CurrentBlock = BlockInfo.Empty;

                if (closure == null)
                {
                    Closure = null;
                    Constants = Tools.Empty<object>();
                    ClosureType = null;
                    ClosureFields = null;
                }
                else
                {
                    Closure = closure;
                    Constants = closureConstantExpressions ?? Tools.Empty<object>();
                    ClosureType = closure.GetType();
                    // todo: verify that Fields types are correspond to `closureConstantExpressions`
                    ClosureFields = ClosureType.GetTypeInfo().DeclaredFields.AsArray();
                }
            }

            public void AddConstant(object expr)
            {
                if (Constants.Length == 0 ||
                    Constants.GetFirstIndex(expr) == -1)
                    Constants = Constants.WithLast(expr);
            }

            public void AddNonPassedParam(object exprObj)
            {
                if (NonPassedParameters.Length == 0 ||
                    NonPassedParameters.GetFirstIndex(exprObj) == -1)
                    NonPassedParameters = NonPassedParameters.WithLast(exprObj);
            }

            public void AddNestedLambda(object lambdaExpr, object lambda, ref ClosureInfo closureInfo, bool isAction)
            {
                if (NestedLambdas.Length == 0 ||
                    NestedLambdas.GetFirstIndex(x => x.LambdaExpr == lambdaExpr) == -1)
                    NestedLambdas = NestedLambdas.WithLast(new NestedLambdaInfo(closureInfo, lambdaExpr, lambda, isAction));
            }

            public void AddNestedLambda(NestedLambdaInfo info)
            {
                if (NestedLambdas.Length == 0 ||
                    NestedLambdas.GetFirstIndex(x => x.LambdaExpr == info.LambdaExpr) == -1)
                    NestedLambdas = NestedLambdas.WithLast(info);
            }

            public object ConstructClosureTypeAndObject(bool constructTypeOnly)
            {
                IsClosureConstructed = true;

                var constants = Constants;
                var nonPassedParams = NonPassedParameters;
                var nestedLambdas = NestedLambdas;
                if (constants.Length == 0 && nonPassedParams.Length == 0 && nestedLambdas.Length == 0)
                    return null;

                var constPlusParamCount = constants.Length + nonPassedParams.Length;
                var totalItemCount = constPlusParamCount + nestedLambdas.Length;

                // Construct the array based closure when number of values is bigger than
                // number of fields in biggest supported Closure class.
                var createMethods = ExpressionCompiler.Closure.CreateMethods;
                if (totalItemCount > createMethods.Length)
                {
                    ClosureType = typeof(ArrayClosure);
                    if (constructTypeOnly)
                        return null;

                    var items = new object[totalItemCount];
                    if (constants.Length != 0)
                        for (var i = 0; i < constants.Length; i++)
                        {
                            var constant = constants[i];
                            var constantExpr = constant as ConstantExpression;
                            items[i] = constantExpr?.Value ?? ((ConstantExpressionInfo)constant).Value;
                        }

                    // skip non passed parameters as it is only for nested lambdas

                    if (nestedLambdas.Length != 0)
                        for (var i = 0; i < nestedLambdas.Length; i++)
                            items[constPlusParamCount + i] = nestedLambdas[i].Lambda;

                    return new ArrayClosure(items);
                }

                // Construct the Closure Type and optionally Closure object with closed values stored as fields:
                object[] fieldValues = null;
                var fieldTypes = new Type[totalItemCount];
                if (constructTypeOnly)
                {
                    if (constants.Length != 0)
                        for (var i = 0; i < constants.Length; i++)
                            fieldTypes[i] = constants[i].GetResultType();

                    if (nonPassedParams.Length != 0)
                        for (var i = 0; i < nonPassedParams.Length; i++)
                            fieldTypes[constants.Length + i] = nonPassedParams[i].GetResultType();

                    if (nestedLambdas.Length != 0)
                        for (var i = 0; i < nestedLambdas.Length; i++)
                            fieldTypes[constPlusParamCount + i] = nestedLambdas[i].Lambda.GetType();
                }
                else
                {
                    fieldValues = new object[totalItemCount];

                    if (constants.Length != 0)
                        for (var i = 0; i < constants.Length; i++)
                        {
                            var constant = constants[i];
                            var constantExpr = constant as ConstantExpression;
                            if (constantExpr != null)
                            {
                                fieldTypes[i] = constantExpr.Type;
                                fieldValues[i] = constantExpr.Value;
                            }
                            else
                            {
                                var constantExprInfo = (ConstantExpressionInfo)constant;
                                fieldTypes[i] = constantExprInfo.Type;
                                fieldValues[i] = constantExprInfo.Value;
                            }
                        }

                    if (nonPassedParams.Length != 0)
                        for (var i = 0; i < nonPassedParams.Length; i++)
                            fieldTypes[constants.Length + i] = nonPassedParams[i].GetResultType();

                    if (nestedLambdas.Length != 0)
                        for (var i = 0; i < nestedLambdas.Length; i++)
                        {
                            var lambda = nestedLambdas[i].Lambda;
                            fieldValues[constPlusParamCount + i] = lambda;
                            fieldTypes[constPlusParamCount + i] = lambda.GetType();
                        }
                }

                var createClosure = createMethods[totalItemCount - 1].MakeGenericMethod(fieldTypes);
                ClosureType = createClosure.ReturnType;
                ClosureFields = ClosureType.GetTypeInfo().DeclaredFields.AsArray();

                return constructTypeOnly ? null : createClosure.Invoke(null, fieldValues);
            }

            public void PushBlock(object blockResultExpr, object[] blockVarExprs, LocalBuilder[] localVars) =>
                CurrentBlock = new BlockInfo(CurrentBlock, blockResultExpr, blockVarExprs, localVars);

            public void PushBlockAndConstructLocalVars(object blockResultExpr, object[] blockVarExprs, ILGenerator il)
            {
                var localVars = Tools.Empty<LocalBuilder>();
                if (blockVarExprs.Length != 0)
                {
                    localVars = new LocalBuilder[blockVarExprs.Length];
                    for (var i = 0; i < localVars.Length; i++)
                        localVars[i] = il.DeclareLocal(blockVarExprs[i].GetResultType());
                }

                CurrentBlock = new BlockInfo(CurrentBlock, blockResultExpr, blockVarExprs, localVars);
            }

            public void PopBlock() =>
                CurrentBlock = CurrentBlock.Parent;

            public bool IsLocalVar(object varParamExpr)
            {
                var i = -1;
                for (var block = CurrentBlock; i == -1 && !block.IsEmpty; block = block.Parent)
                    i = block.VarExprs.GetFirstIndex(varParamExpr);
                return i != -1;
            }

            public LocalBuilder GetDefinedLocalVarOrDefault(object varParamExpr)
            {
                for (var block = CurrentBlock; !block.IsEmpty; block = block.Parent)
                {
                    if (block.LocalVars.Length == 0)
                        continue;
                    var varIndex = block.VarExprs.GetFirstIndex(varParamExpr);
                    if (varIndex != -1)
                        return block.LocalVars[varIndex];
                }
                return null;
            }
        }

        #region Closures

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        static class Closure
        {
            private static readonly IEnumerable<MethodInfo> _methods = typeof(Closure).GetTypeInfo().DeclaredMethods;
            internal static readonly MethodInfo[] CreateMethods = _methods.AsArray();

            public static Closure<T1> Create<T1>(T1 v1) => new Closure<T1>(v1);

            public static Closure<T1, T2> Create<T1, T2>(T1 v1, T2 v2) => new Closure<T1, T2>(v1, v2);

            public static Closure<T1, T2, T3> Create<T1, T2, T3>(T1 v1, T2 v2, T3 v3) =>
                new Closure<T1, T2, T3>(v1, v2, v3);

            public static Closure<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 v1, T2 v2, T3 v3, T4 v4) =>
                new Closure<T1, T2, T3, T4>(v1, v2, v3, v4);

            public static Closure<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 v1, T2 v2, T3 v3, T4 v4,
                T5 v5) => new Closure<T1, T2, T3, T4, T5>(v1, v2, v3, v4, v5);

            public static Closure<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 v1, T2 v2, T3 v3,
                T4 v4, T5 v5, T6 v6) => new Closure<T1, T2, T3, T4, T5, T6>(v1, v2, v3, v4, v5, v6);

            public static Closure<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 v1, T2 v2,
                T3 v3, T4 v4, T5 v5, T6 v6, T7 v7) =>
                new Closure<T1, T2, T3, T4, T5, T6, T7>(v1, v2, v3, v4, v5, v6, v7);

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8> Create<T1, T2, T3, T4, T5, T6, T7, T8>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8) =>
                new Closure<T1, T2, T3, T4, T5, T6, T7, T8>(v1, v2, v3, v4, v5, v6, v7, v8);

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9) =>
                new Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9>(v1, v2, v3, v4, v5, v6, v7, v8, v9);

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10) =>
                new Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10);
        }

        sealed class Closure<T1>
        {
            public T1 V1;
            public Closure(T1 v1) { V1 = v1; }
        }

        sealed class Closure<T1, T2>
        {
            public T1 V1;
            public T2 V2;
            public Closure(T1 v1, T2 v2) { V1 = v1; V2 = v2; }
        }

        sealed class Closure<T1, T2, T3>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public Closure(T1 v1, T2 v2, T3 v3) { V1 = v1; V2 = v2; V3 = v3; }
        }

        sealed class Closure<T1, T2, T3, T4>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; }
        }

        sealed class Closure<T1, T2, T3, T4, T5>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; }
        }

        sealed class Closure<T1, T2, T3, T4, T5, T6>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; }
        }

        sealed class Closure<T1, T2, T3, T4, T5, T6, T7>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; V7 = v7; }
        }

        sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public T8 V8;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; V7 = v7; V8 = v8; }
        }

        sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public T8 V8;
            public T9 V9;

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; V7 = v7; V8 = v8; V9 = v9; }
        }

        sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public T8 V8;
            public T9 V9;
            public T10 V10;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; V7 = v7; V8 = v8; V9 = v9; V10 = v10; }
        }

        sealed class ArrayClosure
        {
            public readonly object[] Constants;

            public static FieldInfo ArrayField = typeof(ArrayClosure).GetTypeInfo().GetDeclaredField(nameof(Constants));
            public static ConstructorInfo Constructor = typeof(ArrayClosure).GetTypeInfo().DeclaredConstructors.GetFirst();

            public ArrayClosure(object[] constants) { Constants = constants; }
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion

        #region Nested Lambdas

        private struct NestedLambdaInfo
        {
            public readonly ClosureInfo ClosureInfo;

            public readonly object LambdaExpr; // to find the lambda in bigger parent expression
            public readonly object Lambda;
            public readonly bool IsAction;

            public NestedLambdaInfo(ClosureInfo closureInfo, object lambdaExpr, object lambda, bool isAction)
            {
                ClosureInfo = closureInfo;
                Lambda = lambda;
                LambdaExpr = lambdaExpr;
                IsAction = isAction;
            }
        }

        internal static class CurryClosureFuncs
        {
            private static readonly IEnumerable<MethodInfo> _methods =
                typeof(CurryClosureFuncs).GetTypeInfo().DeclaredMethods;

            public static readonly MethodInfo[] Methods = _methods.AsArray();

            public static Func<R> Curry<C, R>(Func<C, R> f, C c) => () => f(c);
            public static Func<T1, R> Curry<C, T1, R>(Func<C, T1, R> f, C c) => t1 => f(c, t1);
            public static Func<T1, T2, R> Curry<C, T1, T2, R>(Func<C, T1, T2, R> f, C c) => (t1, t2) => f(c, t1, t2);
            public static Func<T1, T2, T3, R> Curry<C, T1, T2, T3, R>(Func<C, T1, T2, T3, R> f, C c) => (t1, t2, t3) => f(c, t1, t2, t3);
            public static Func<T1, T2, T3, T4, R> Curry<C, T1, T2, T3, T4, R>(Func<C, T1, T2, T3, T4, R> f, C c) => (t1, t2, t3, t4) => f(c, t1, t2, t3, t4);
            public static Func<T1, T2, T3, T4, T5, R> Curry<C, T1, T2, T3, T4, T5, R>(Func<C, T1, T2, T3, T4, T5, R> f, C c) => (t1, t2, t3, t4, t5) => f(c, t1, t2, t3, t4, t5);
            public static Func<T1, T2, T3, T4, T5, T6, R> Curry<C, T1, T2, T3, T4, T5, T6, R>(Func<C, T1, T2, T3, T4, T5, T6, R> f, C c) => (t1, t2, t3, t4, t5, t6) => f(c, t1, t2, t3, t4, t5, t6);
        }

        internal static class CurryClosureActions
        {
            private static readonly IEnumerable<MethodInfo> _methods =
                typeof(CurryClosureActions).GetTypeInfo().DeclaredMethods;

            public static readonly MethodInfo[] Methods = _methods.AsArray();

            internal static Action Curry<C>(Action<C> a, C c) => () => a(c);
            internal static Action<T1> Curry<C, T1>(Action<C, T1> f, C c) => t1 => f(c, t1);
            internal static Action<T1, T2> Curry<C, T1, T2>(Action<C, T1, T2> f, C c) => (t1, t2) => f(c, t1, t2);
            internal static Action<T1, T2, T3> Curry<C, T1, T2, T3>(Action<C, T1, T2, T3> f, C c) => (t1, t2, t3) => f(c, t1, t2, t3);
            internal static Action<T1, T2, T3, T4> Curry<C, T1, T2, T3, T4>(Action<C, T1, T2, T3, T4> f, C c) => (t1, t2, t3, t4) => f(c, t1, t2, t3, t4);
            internal static Action<T1, T2, T3, T4, T5> Curry<C, T1, T2, T3, T4, T5>(Action<C, T1, T2, T3, T4, T5> f, C c) => (t1, t2, t3, t4, t5) => f(c, t1, t2, t3, t4, t5);
            internal static Action<T1, T2, T3, T4, T5, T6> Curry<C, T1, T2, T3, T4, T5, T6>(Action<C, T1, T2, T3, T4, T5, T6> f, C c) => (t1, t2, t3, t4, t5, t6) => f(c, t1, t2, t3, t4, t5, t6);
        }

        #endregion

        #region Collect Bound Constants

        private static bool IsClosureBoundConstant(object value, TypeInfo type) => 
            value is Delegate ||
            !type.IsPrimitive && !type.IsEnum && !(value is string) && !(value is Type);

        // @paramExprs is required for nested lambda compilation
        private static bool TryCollectBoundConstants(ref ClosureInfo closure,
            object exprObj, ExpressionType exprNodeType, object[] paramExprs)
        {
            if (exprObj == null)
                return false;

            switch (exprNodeType)
            {
                case ExpressionType.Constant:
                    var constExprInfo = exprObj as ConstantExpressionInfo;
                    var value = constExprInfo != null ? constExprInfo.Value : ((ConstantExpression)exprObj).Value;
                    if (value != null && IsClosureBoundConstant(value, value.GetType().GetTypeInfo()))
                        closure.AddConstant(exprObj);
                    return true;

                case ExpressionType.Parameter:
                    // if parameter is used BUT is not in passed parameters and not in local variables,
                    // it means parameter is provided by outer lambda and should be put in closure for current lambda
                    if (paramExprs.GetFirstIndex(exprObj) == -1 && !closure.IsLocalVar(exprObj))
                        closure.AddNonPassedParam(exprObj);
                    return true;

                case ExpressionType.Call:
                    return TryCollectCallExprConstants(ref closure, exprObj, paramExprs);

                case ExpressionType.MemberAccess:
                    var memberExprInfo = exprObj as MemberExpressionInfo;
                    if (memberExprInfo != null)
                    {
                        var maExpr = memberExprInfo.Expression;
                        return maExpr == null
                            || TryCollectBoundConstants(ref closure, maExpr, maExpr.GetNodeType(), paramExprs);
                    }

                    var memberExpr = ((MemberExpression)exprObj).Expression;
                    return memberExpr == null
                        || TryCollectBoundConstants(ref closure, memberExpr, memberExpr.NodeType, paramExprs);

                case ExpressionType.New:
                    var newExprInfo = exprObj as NewExpressionInfo;
                    return newExprInfo != null
                        ? TryCollectBoundConstants(ref closure, newExprInfo.Arguments, paramExprs)
                        : TryCollectBoundConstants(ref closure, ((NewExpression)exprObj).Arguments, paramExprs);

                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                    var newArrayInitInfo = exprObj as NewArrayExpressionInfo;
                    return newArrayInitInfo != null ?
                        TryCollectBoundConstants(ref closure, newArrayInitInfo.Arguments, paramExprs) :
                        TryCollectBoundConstants(ref closure, ((NewArrayExpression)exprObj).Expressions, paramExprs);

                case ExpressionType.MemberInit:
                    return TryCollectMemberInitExprConstants(ref closure, exprObj, paramExprs);

                case ExpressionType.Lambda:
                    return TryCompileNestedLambda(ref closure, exprObj, paramExprs);

                case ExpressionType.Invoke:
                    var invokeExpr = exprObj as InvocationExpression;
                    if (invokeExpr != null)
                    {
                        var lambda = invokeExpr.Expression;
                        return TryCollectBoundConstants(ref closure, lambda, lambda.NodeType, paramExprs)
                            && TryCollectBoundConstants(ref closure, invokeExpr.Arguments, paramExprs);
                    }
                    else
                    {
                        var invokeInfo = (InvocationExpressionInfo)exprObj;
                        var lambda = invokeInfo.ExprToInvoke;
                        return TryCollectBoundConstants(ref closure, lambda, lambda.NodeType, paramExprs)
                            && TryCollectBoundConstants(ref closure, invokeInfo.Arguments, paramExprs);
                    }

                case ExpressionType.Conditional:
                    var condExpr = (ConditionalExpression)exprObj;
                    return TryCollectBoundConstants(ref closure, condExpr.Test, condExpr.Test.NodeType, paramExprs)
                        && TryCollectBoundConstants(ref closure, condExpr.IfTrue, condExpr.IfTrue.NodeType, paramExprs)
                        && TryCollectBoundConstants(ref closure, condExpr.IfFalse, condExpr.IfFalse.NodeType, paramExprs);

                case ExpressionType.Block:
                    return TryCollectBlockBoundConstants(ref closure, exprObj, paramExprs);

                case ExpressionType.Index:
                    var indexExpr = (IndexExpression)exprObj;
                    var obj = indexExpr.Object;
                    return obj == null
                        || TryCollectBoundConstants(ref closure, indexExpr.Object, indexExpr.Object.NodeType, paramExprs)
                        && TryCollectBoundConstants(ref closure, indexExpr.Arguments, paramExprs);

                case ExpressionType.Try:
                    return exprObj is TryExpression
                        ? TryCollectTryExprConstants(ref closure, (TryExpression)exprObj, paramExprs)
                        : TryCollectTryExprInfoConstants(ref closure, (TryExpressionInfo)exprObj, paramExprs);

                case ExpressionType.Default:
                    return true;

                default:
                    return TryCollectUnaryOrBinaryExprConstants(ref closure, exprObj, paramExprs);
            }
        }

        private static bool TryCollectBlockBoundConstants(ref ClosureInfo closure, object exprObj, object[] paramExprs)
        {
            var blockExpr = exprObj as BlockExpression;
            if (blockExpr != null)
            {
                closure.PushBlock(blockExpr.Result, blockExpr.Variables.AsArray(), Tools.Empty<LocalBuilder>());
                if (!TryCollectBoundConstants(ref closure, blockExpr.Expressions, paramExprs))
                    return false;
            }
            else
            {
                var blockExprInfo = (BlockExpressionInfo)exprObj;
                closure.PushBlock(blockExprInfo.Result, blockExprInfo.Variables, Tools.Empty<LocalBuilder>());
                if (!TryCollectBoundConstants(ref closure, blockExprInfo.Expressions, paramExprs))
                    return false;
            }

            closure.PopBlock();
            return true;
        }

        private static bool TryCollectBoundConstants(ref ClosureInfo closure, object[] exprObjects, object[] paramExprs)
        {
            for (var i = 0; i < exprObjects.Length; i++)
            {
                var exprObj = exprObjects[i];
                if (!TryCollectBoundConstants(ref closure, exprObj, exprObj.GetNodeType(), paramExprs))
                    return false;
            }
            return true;
        }

        private static bool TryCompileNestedLambda(ref ClosureInfo closure, object exprObj, object[] paramExprs)
        {
            // 1. Try to compile nested lambda in place
            // 2. Check that parameters used in compiled lambda are passed or closed by outer lambda
            // 3. Add the compiled lambda to closure of outer lambda for later invocation

            object compiledLambda;
            Type bodyType;
            var nestedClosure = new ClosureInfo(false);

            var lambdaExprInfo = exprObj as LambdaExpressionInfo;
            if (lambdaExprInfo != null)
            {
                var lambdaParamExprs = lambdaExprInfo.Parameters;
                var bodyExpr = lambdaExprInfo.Body;
                bodyType = bodyExpr.GetResultType();
                compiledLambda = TryCompile(ref nestedClosure,
                    lambdaExprInfo.Type, Tools.GetParamExprTypes(lambdaParamExprs), bodyType,
                    bodyExpr, bodyExpr.GetNodeType(), bodyType,
                    lambdaParamExprs, isNestedLambda: true);
            }
            else
            {
                var lambdaExpr = (LambdaExpression)exprObj;
                object[] lambdaParamExprs = lambdaExpr.Parameters.AsArray();
                var bodyExpr = lambdaExpr.Body;
                bodyType = bodyExpr.Type;
                compiledLambda = TryCompile(ref nestedClosure,
                    lambdaExpr.Type, Tools.GetParamExprTypes(lambdaParamExprs), bodyType,
                    bodyExpr, bodyExpr.NodeType, bodyExpr.Type,
                    lambdaParamExprs, isNestedLambda: true);
            }

            if (compiledLambda == null)
                return false;

            // add the nested lambda into closure
            closure.AddNestedLambda(exprObj, compiledLambda, ref nestedClosure, isAction: bodyType == typeof(void));

            if (!nestedClosure.HasClosure)
                return true; // no closure, we are done

            // if nested non passed parameter is no matched with any outer passed parameter, 
            // then ensure it goes to outer non passed parameter.
            // But check that having a non-passed parameter in root expression is invalid.
            var nestedNonPassedParams = nestedClosure.NonPassedParameters;
            if (nestedNonPassedParams.Length != 0)
                for (var i = 0; i < nestedNonPassedParams.Length; i++)
                {
                    var nestedNonPassedParam = nestedNonPassedParams[i];
                    if (paramExprs.GetFirstIndex(nestedNonPassedParam) == -1)
                        closure.AddNonPassedParam(nestedNonPassedParam);
                }

            // Promote found constants and nested lambdas into outer closure
            var nestedConstants = nestedClosure.Constants;
            if (nestedConstants.Length != 0)
                for (var i = 0; i < nestedConstants.Length; i++)
                    closure.AddConstant(nestedConstants[i]);

            var nestedNestedLambdas = nestedClosure.NestedLambdas;
            if (nestedNestedLambdas.Length != 0)
                for (var i = 0; i < nestedNestedLambdas.Length; i++)
                    closure.AddNestedLambda(nestedNestedLambdas[i]);

            return true;
        }

        private static bool TryCollectMemberInitExprConstants(ref ClosureInfo closure, object exprObj, object[] paramExprs)
        {
            var memberInitExprInfo = exprObj as MemberInitExpressionInfo;
            if (memberInitExprInfo != null)
            {
                var miNewInfo = memberInitExprInfo.ExpressionInfo;
                if (!TryCollectBoundConstants(ref closure, miNewInfo, miNewInfo.NodeType, paramExprs))
                    return false;

                var memberBindingInfos = memberInitExprInfo.Bindings;
                for (var i = 0; i < memberBindingInfos.Length; i++)
                {
                    var maInfo = memberBindingInfos[i].Expression;
                    if (!TryCollectBoundConstants(ref closure, maInfo, maInfo.NodeType, paramExprs))
                        return false;
                }
                return true;
            }
            else
            {
                var memberInitExpr = (MemberInitExpression)exprObj;
                var miNewExpr = memberInitExpr.NewExpression;
                if (!TryCollectBoundConstants(ref closure, miNewExpr, miNewExpr.NodeType, paramExprs))
                    return false;

                var memberBindings = memberInitExpr.Bindings;
                for (var i = 0; i < memberBindings.Count; ++i)
                {
                    var memberBinding = memberBindings[i];
                    var maExpr = ((MemberAssignment)memberBinding).Expression;
                    if (memberBinding.BindingType == MemberBindingType.Assignment &&
                        !TryCollectBoundConstants(ref closure, maExpr, maExpr.NodeType, paramExprs))
                        return false;
                }
            }

            return true;
        }

        private static bool TryCollectTryExprConstants(ref ClosureInfo closure, TryExpression tryExpr, object[] paramExprs)
        {
            if (!TryCollectBoundConstants(ref closure, tryExpr.Body, tryExpr.Body.NodeType, paramExprs))
                return false;

            var catchBlocks = tryExpr.Handlers;
            for (var i = 0; i < catchBlocks.Count; i++)
            {
                var catchBlock = catchBlocks[i];
                var catchBody = catchBlock.Body;
                var catchExVar = catchBlock.Variable;
                if (catchExVar != null)
                {
                    closure.PushBlock(catchBody, new[] { catchExVar }, Tools.Empty<LocalBuilder>());
                    if (!TryCollectBoundConstants(ref closure, catchExVar, catchExVar.NodeType, paramExprs))
                        return false;
                }

                var filterExpr = catchBlock.Filter;
                if (filterExpr != null &&
                    !TryCollectBoundConstants(ref closure, filterExpr, filterExpr.NodeType, paramExprs))
                    return false;

                if (!TryCollectBoundConstants(ref closure, catchBody, catchBody.NodeType, paramExprs))
                    return false;

                if (catchExVar != null)
                    closure.PopBlock();
            }

            var finallyExpr = tryExpr.Finally;
            return finallyExpr == null
                || TryCollectBoundConstants(ref closure, finallyExpr, finallyExpr.NodeType, paramExprs);
        }

        private static bool TryCollectTryExprInfoConstants(ref ClosureInfo closure, TryExpressionInfo tryExpr, object[] paramExprs)
        {
            if (!TryCollectBoundConstants(ref closure, tryExpr.Body, tryExpr.Body.GetNodeType(), paramExprs))
                return false;

            var catchBlocks = tryExpr.Handlers;
            for (var i = 0; i < catchBlocks.Length; i++)
            {
                var catchBlock = catchBlocks[i];
                var catchBody = catchBlock.Body;
                var catchExVar = catchBlock.Variable;
                if (catchExVar != null)
                {
                    closure.PushBlock(catchBody, new[] { catchExVar }, Tools.Empty<LocalBuilder>());
                    if (!TryCollectBoundConstants(ref closure, catchExVar, catchExVar.NodeType, paramExprs))
                        return false;
                }

                var filterExpr = catchBlock.Filter;
                if (filterExpr != null &&
                    !TryCollectBoundConstants(ref closure, filterExpr, filterExpr.NodeType, paramExprs))
                    return false;

                if (!TryCollectBoundConstants(ref closure, catchBody, catchBody.NodeType, paramExprs))
                    return false;

                if (catchExVar != null)
                    closure.PopBlock();
            }

            var finallyExpr = tryExpr.Finally;
            return finallyExpr == null || TryCollectBoundConstants(ref closure, finallyExpr, finallyExpr.NodeType, paramExprs);
        }

        private static bool TryCollectUnaryOrBinaryExprConstants(ref ClosureInfo closure, object exprObj, object[] paramExprs)
        {
            if (exprObj is ExpressionInfo)
            {
                var unaryExprInfo = exprObj as UnaryExpressionInfo;
                if (unaryExprInfo != null)
                    return TryCollectBoundConstants(ref closure, unaryExprInfo.Operand, unaryExprInfo.Operand.NodeType, paramExprs);

                var binInfo = exprObj as BinaryExpressionInfo;
                if (binInfo != null)
                    return TryCollectBoundConstants(ref closure, binInfo.Left, binInfo.Left.GetNodeType(), paramExprs)
                        && TryCollectBoundConstants(ref closure, binInfo.Right, binInfo.Right.GetNodeType(), paramExprs);

                return false;
            }

            var unaryExpr = exprObj as UnaryExpression;
            if (unaryExpr != null)
                return TryCollectBoundConstants(ref closure, unaryExpr.Operand, unaryExpr.Operand.NodeType, paramExprs);

            var binaryExpr = exprObj as BinaryExpression;
            if (binaryExpr != null)
                return TryCollectBoundConstants(ref closure, binaryExpr.Left, binaryExpr.Left.NodeType, paramExprs)
                    && TryCollectBoundConstants(ref closure, binaryExpr.Right, binaryExpr.Right.NodeType, paramExprs);

            return false;
        }

        private static bool TryCollectCallExprConstants(ref ClosureInfo closure, object exprObj, object[] paramExprs)
        {
            var callInfo = exprObj as MethodCallExpressionInfo;
            if (callInfo != null)
            {
                var objInfo = callInfo.Object;
                return (objInfo == null
                    || TryCollectBoundConstants(ref closure, objInfo, objInfo.NodeType, paramExprs))
                    && TryCollectBoundConstants(ref closure, callInfo.Arguments, paramExprs);
            }

            var callExpr = (MethodCallExpression)exprObj;
            var objExpr = callExpr.Object;
            return (objExpr == null
                || TryCollectBoundConstants(ref closure, objExpr, objExpr.NodeType, paramExprs))
                && TryCollectBoundConstants(ref closure, callExpr.Arguments, paramExprs);
        }

        private static bool TryCollectBoundConstants(ref ClosureInfo closure, IList<Expression> exprs, object[] paramExprs)
        {
            for (var i = 0; i < exprs.Count; i++)
            {
                var expr = exprs[i];
                if (!TryCollectBoundConstants(ref closure, expr, expr.NodeType, paramExprs))
                    return false;
            }
            return true;
        }

        #endregion

        /// <summary>Supports emitting of selected expressions, e.g. lambdaExpr are not supported yet.
        /// When emitter find not supported expression it will return false from <see cref="TryEmit"/>, so I could fallback
        /// to normal and slow Expression.Compile.</summary>
        private static class EmittingVisitor
        {
            private static readonly MethodInfo _getTypeFromHandleMethod = typeof(Type).GetTypeInfo()
                .DeclaredMethods.First(m => m.IsStatic && m.Name == "GetTypeFromHandle");

            private static readonly MethodInfo _objectEqualsMethod = typeof(object).GetTypeInfo()
                .DeclaredMethods.First(m => m.IsStatic && m.Name == "Equals");

            public static bool TryEmit(object exprObj, ExpressionType exprNodeType, Type exprType,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent)
            {
                switch (exprNodeType)
                {
                    case ExpressionType.Parameter:
                        return TryEmitParameter(exprObj, exprType, paramExprs, il, ref closure, parent);
                    case ExpressionType.Convert:
                        return TryEmitConvert(exprObj, exprType, paramExprs, il, ref closure);
                    case ExpressionType.ArrayIndex:
                        return TryEmitArrayIndex(exprObj, exprType, paramExprs, il, ref closure);
                    case ExpressionType.Constant:
                        return TryEmitConstant(exprObj, exprType, il, ref closure);
                    case ExpressionType.Call:
                        return TryEmitMethodCall(exprObj, paramExprs, il, ref closure);
                    case ExpressionType.MemberAccess:
                        return TryEmitMemberAccess(exprObj, paramExprs, il, ref closure);
                    case ExpressionType.New:
                        return TryEmitNew(exprObj, exprType, paramExprs, il, ref closure);
                    case ExpressionType.NewArrayBounds:
                    case ExpressionType.NewArrayInit:
                        return EmitNewArray(exprObj, exprType, paramExprs, il, ref closure);
                    case ExpressionType.MemberInit:
                        return EmitMemberInit(exprObj, exprType, paramExprs, il, ref closure, parent);
                    case ExpressionType.Lambda:
                        return TryEmitNestedLambda(exprObj, paramExprs, il, ref closure);

                    case ExpressionType.Invoke:
                        return TryInvokeLambda(exprObj, paramExprs, il, ref closure);

                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        return TryEmitComparison(exprObj, exprNodeType, paramExprs, il, ref closure);

                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Divide:
                        return TryEmitArithmeticOperation(exprObj, exprType, exprNodeType, paramExprs, il, ref closure);

                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                        return TryEmitLogicalOperator((BinaryExpression)exprObj, paramExprs, il, ref closure);

                    case ExpressionType.Coalesce:
                        return TryEmitCoalesceOperator((BinaryExpression)exprObj, paramExprs, il, ref closure);

                    case ExpressionType.Conditional:
                        return TryEmitConditional((ConditionalExpression)exprObj, paramExprs, il, ref closure);

                    case ExpressionType.Assign:
                        return TryEmitAssign(exprObj, exprType, paramExprs, il, ref closure);

                    case ExpressionType.Block:
                        return exprObj is BlockExpression ?
                            TryEmitBlock((BlockExpression)exprObj, paramExprs, il, ref closure) :
                            TryEmitBlockInfo((BlockExpressionInfo)exprObj, paramExprs, il, ref closure);

                    case ExpressionType.Try:
                        return exprObj is TryExpression
                            ? TryEmitTryCatchFinallyBlock((TryExpression)exprObj, exprType, paramExprs, il, ref closure)
                            : TryEmitTryCatchFinallyBlockInfo((TryExpressionInfo)exprObj, exprType, paramExprs, il, ref closure);

                    case ExpressionType.Throw:
                        return TryEmitThrow(exprObj, paramExprs, il, ref closure);

                    case ExpressionType.Default:
                        return exprType == typeof(void) || EmitDefault(exprType, il);

                    case ExpressionType.Index:
                        return TryEmitIndex((IndexExpression)exprObj, exprType, paramExprs, il, ref closure);

                    default:
                        return false;
                }
            }

            private static bool TryEmitIndex(IndexExpression exprObj, Type elemType,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var obj = exprObj.Object;
                if (obj != null && !TryEmit(obj, obj.NodeType, obj.Type, paramExprs, il, ref closure, ExpressionType.Index))
                    return false;
                    
                var argLength = exprObj.Arguments.Count;
                for (var i = 0; i < argLength; i++)
                {
                    var arg = exprObj.Arguments[i];
                    if (!TryEmit(arg, arg.NodeType, arg.Type, paramExprs, il, ref closure, ExpressionType.Index))
                        return false;
                }

                var instType = obj?.Type;
                if (exprObj.Indexer != null)
                {
                    var propGetMethod = TryGetPropertyGetMethod(exprObj.Indexer);
                    return propGetMethod != null && EmitMethodCall(il, propGetMethod);
                }

                if (exprObj.Arguments.Count == 1) // one dimensional array
                {
                    if (elemType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Ldelem, elemType);
                    else
                        il.Emit(OpCodes.Ldelem_Ref);
                    return true;
                }
                
                // multi dimensional array
                var getMethod = instType?.GetTypeInfo().GetDeclaredMethod("Get");
                return getMethod != null && EmitMethodCall(il, getMethod);
            }

            private static bool TryEmitCoalesceOperator(BinaryExpression exprObj, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var labelFalse = il.DefineLabel();
                var labelDone = il.DefineLabel();

                var left = exprObj.Left;
                var right = exprObj.Right;

                if (!TryEmit(left, left.NodeType, left.Type, paramExprs, il, ref closure, ExpressionType.Coalesce))
                    return false;

                il.Emit(OpCodes.Dup); // duplicate left, if it's not null, after the branch this value will be on the top of the stack
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, labelFalse);

                il.Emit(OpCodes.Pop); // left is null, pop its value from the stack

                if (!TryEmit(right, right.NodeType, right.Type, paramExprs, il, ref closure, ExpressionType.Coalesce))
                    return false;

                if (right.Type != exprObj.Type)
                    if (right.Type.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Box, right.Type);
                    else
                        il.Emit(OpCodes.Castclass, exprObj.Type);

                il.Emit(OpCodes.Br, labelDone);

                il.MarkLabel(labelFalse);
                if (left.Type != exprObj.Type)
                    il.Emit(OpCodes.Castclass, exprObj.Type);

                il.MarkLabel(labelDone);
                return true;
            }

            private static bool EmitDefault(Type type, ILGenerator il)
            {
                if (type == typeof(string))
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (
                    type == typeof(bool) ||
                    type == typeof(byte) ||
                    type == typeof(char) ||
                    type == typeof(sbyte) ||
                    type == typeof(int) ||
                    type == typeof(uint) ||
                    type == typeof(short) ||
                    type == typeof(ushort))
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                }
                else if (
                    type == typeof(long) ||
                    type == typeof(ulong))
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_I8);
                }
                else if (type == typeof(float))
                    il.Emit(OpCodes.Ldc_R4, default(float));
                else if (type == typeof(double))
                    il.Emit(OpCodes.Ldc_R8, default(double));
                else if (type.GetTypeInfo().IsValueType)
                    il.Emit(OpCodes.Ldloc, InitValueTypeVariable(il, type));
                else
                    il.Emit(OpCodes.Ldnull);

                return true;
            }

            private static bool TryEmitBlock(BlockExpression blockExpr, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                closure.PushBlockAndConstructLocalVars(blockExpr.Result, blockExpr.Variables.AsArray(), il);
                var ok = EmitMany(blockExpr.Expressions, paramExprs, il, ref closure, ExpressionType.Block);
                closure.PopBlock();
                return ok;
            }

            private static bool TryEmitBlockInfo(BlockExpressionInfo blockExpr, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                closure.PushBlockAndConstructLocalVars(blockExpr.Result, blockExpr.Variables, il);
                var ok = EmitMany(blockExpr.Expressions, paramExprs, il, ref closure, ExpressionType.Block);
                closure.PopBlock();
                return ok;
            }

            private static bool TryEmitTryCatchFinallyBlock(TryExpression tryExpr, Type exprType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var returnLabel = default(Label);
                var returnResult = default(LocalBuilder);
                var isNonVoid = exprType != typeof(void);
                if (isNonVoid)
                {
                    returnLabel = il.DefineLabel();
                    returnResult = il.DeclareLocal(exprType);
                }

                il.BeginExceptionBlock();
                var tryBodyExpr = tryExpr.Body;
                if (!TryEmit(tryBodyExpr, tryBodyExpr.NodeType, tryBodyExpr.Type, paramExprs, il, ref closure, ExpressionType.Try))
                    return false;

                if (isNonVoid)
                {
                    il.Emit(OpCodes.Stloc_S, returnResult);
                    il.Emit(OpCodes.Leave_S, returnLabel);
                }

                var catchBlocks = tryExpr.Handlers;
                for (var i = 0; i < catchBlocks.Count; i++)
                {
                    var catchBlock = catchBlocks[i];
                    if (catchBlock.Filter != null)
                        return false; // todo: Add support for filters on catch expression

                    il.BeginCatchBlock(catchBlock.Test);

                    // at the beginning of catch the Exception value is on the stack,
                    // we will store into local variable.
                    var catchBodyExpr = catchBlock.Body;
                    var exVarExpr = catchBlock.Variable;
                    if (exVarExpr != null)
                    {
                        var exVar = il.DeclareLocal(exVarExpr.Type);
                        closure.PushBlock(catchBodyExpr, new[] { exVarExpr }, new[] { exVar });
                        il.Emit(OpCodes.Stloc_S, exVar);
                    }

                    if (!TryEmit(catchBodyExpr, catchBodyExpr.NodeType, catchBodyExpr.Type, paramExprs, il, ref closure, ExpressionType.Try))
                        return false;

                    if (exVarExpr != null)
                        closure.PopBlock();

                    if (isNonVoid)
                    {
                        il.Emit(OpCodes.Stloc_S, returnResult);
                        il.Emit(OpCodes.Leave_S, returnLabel);
                    }
                    else
                        il.Emit(OpCodes.Pop);
                }

                var finallyExpr = tryExpr.Finally;
                if (finallyExpr != null)
                {
                    il.BeginFinallyBlock();
                    if (!TryEmit(finallyExpr, finallyExpr.NodeType, finallyExpr.Type, paramExprs, il, ref closure, ExpressionType.Try))
                        return false;
                }

                il.EndExceptionBlock();
                if (isNonVoid)
                {
                    il.MarkLabel(returnLabel);
                    il.Emit(OpCodes.Ldloc, returnResult);
                }

                return true;
            }

            private static bool TryEmitTryCatchFinallyBlockInfo(TryExpressionInfo tryExpr, Type exprType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var returnLabel = default(Label);
                var returnResult = default(LocalBuilder);
                var isNonVoid = exprType != typeof(void);
                if (isNonVoid)
                {
                    returnLabel = il.DefineLabel();
                    returnResult = il.DeclareLocal(exprType);
                }

                il.BeginExceptionBlock();
                var bodyExpr = tryExpr.Body;
                if (!TryEmit(bodyExpr, bodyExpr.GetNodeType(), bodyExpr.GetResultType(), paramExprs, il, ref closure, ExpressionType.Try))
                    return false;

                if (isNonVoid)
                {
                    il.Emit(OpCodes.Stloc_S, returnResult);
                    il.Emit(OpCodes.Leave_S, returnLabel);
                }

                var catchBlocks = tryExpr.Handlers;
                for (var i = 0; i < catchBlocks.Length; i++)
                {
                    var catchBlock = catchBlocks[i];
                    if (catchBlock.Filter != null)
                        return false; // todo: Add support for filters on catch expression

                    il.BeginCatchBlock(catchBlock.Test);

                    // at the beginning of catch the Exception value is on the stack,
                    // we will store into local variable.
                    var catchBodyExpr = catchBlock.Body;
                    var exVarExpr = catchBlock.Variable;
                    if (exVarExpr != null)
                    {
                        var exVar = il.DeclareLocal(exVarExpr.Type);
                        closure.PushBlock(catchBodyExpr, new[] { exVarExpr }, new[] { exVar });
                        il.Emit(OpCodes.Stloc_S, exVar);
                    }

                    if (!TryEmit(catchBodyExpr, catchBodyExpr.NodeType, catchBodyExpr.Type, paramExprs, il, ref closure, ExpressionType.Try))
                        return false;

                    if (exVarExpr != null)
                        closure.PopBlock();

                    if (isNonVoid)
                    {
                        il.Emit(OpCodes.Stloc_S, returnResult);
                        il.Emit(OpCodes.Leave_S, returnLabel);
                    }
                    else
                        il.Emit(OpCodes.Pop);
                }

                var finallyExpr = tryExpr.Finally;
                if (finallyExpr != null)
                {
                    il.BeginFinallyBlock();
                    if (!TryEmit(finallyExpr, finallyExpr.NodeType, finallyExpr.Type, paramExprs, il, ref closure, ExpressionType.Try))
                        return false;
                }

                il.EndExceptionBlock();
                if (isNonVoid)
                {
                    il.MarkLabel(returnLabel);
                    il.Emit(OpCodes.Ldloc, returnResult);
                }

                return true;
            }

            private static bool TryEmitThrow(object exprObj,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var ex = exprObj.GetOperandExprInfo();
                var ok = TryEmit(ex.Expr, ex.NodeType, ex.Type, paramExprs, il, ref closure, ExpressionType.Throw);
                il.ThrowException(ex.Type);
                return ok;
            }

            private static bool TryEmitParameter(object paramExprObj, Type paramType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent)
            {
                // ref, and out parameters are not supported yet
                if ((paramExprObj as ParameterExpression)?.IsByRef == true)
                    return false;

                // if parameter is passed, then just load it on stack
                var paramIndex = paramExprs.GetFirstIndex(paramExprObj);
                if (paramIndex != -1)
                {
                    if (closure.HasClosure)
                        paramIndex += 1; // shift parameter indices by one, because the first one will be closure

                    var asAddress = parent == ExpressionType.Call && paramType.GetTypeInfo().IsValueType;
                    LoadParamArg(il, paramIndex, asAddress);
                    return true;
                }

                // if parameter isn't passed, then it is passed into some outer lambda or it is a local variable,
                // so it should be loaded from closure or from the locals. Then the closure is null will be an invalid state.
                if (!closure.IsClosureConstructed)
                    return false;

                // parameter may represent a variable, so first look if this is the case
                var variable = closure.GetDefinedLocalVarOrDefault(paramExprObj);
                if (variable != null)
                {
                    il.Emit(OpCodes.Ldloc, variable);
                    return true;
                }

                // the only possibility that we are here is because we are in nested lambda,
                // and it uses some parameter or variable from the outer lambda
                var nonPassedParamIndex = closure.NonPassedParameters.GetFirstIndex(paramExprObj);
                if (nonPassedParamIndex == -1)
                    return false;  // what??? no chance

                var closureItemIndex = closure.Constants.Length + nonPassedParamIndex;
                LoadClosureFieldOrItem(ref closure, il, closureItemIndex, paramType);

                return true;
            }

            private static void LoadParamArg(ILGenerator il, int paramIndex, bool asAddress)
            {
                if (asAddress)
                {
                    if (paramIndex <= byte.MaxValue)
                        il.Emit(OpCodes.Ldarga_S, (byte)paramIndex);
                    else
                        il.Emit(OpCodes.Ldarga, paramIndex);
                    return;
                }

                switch (paramIndex)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        if (paramIndex <= byte.MaxValue)
                            il.Emit(OpCodes.Ldarg_S, (byte)paramIndex);
                        else
                            il.Emit(OpCodes.Ldarg, paramIndex);
                        break;
                }
            }

            private static bool EmitBinary(object exprObj, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent)
            {
                var exprInfo = exprObj as BinaryExpressionInfo;
                if (exprInfo != null)
                {
                    var left = exprInfo.Left.GetExprInfo();
                    var right = exprInfo.Right.GetExprInfo();
                    return TryEmit(left.Expr, left.NodeType, left.Type, paramExprs, il, ref closure, parent)
                        && TryEmit(right.Expr, right.NodeType, right.Type, paramExprs, il, ref closure, parent);
                }

                var expr = (BinaryExpression)exprObj;
                var leftExpr = expr.Left;
                var rightExpr = expr.Right;
                return TryEmit(leftExpr, leftExpr.NodeType, leftExpr.Type, paramExprs, il, ref closure, parent)
                    && TryEmit(rightExpr, rightExpr.NodeType, rightExpr.Type, paramExprs, il, ref closure, parent);
            }

            private static bool EmitMany(IList<Expression> exprs, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent)
            {
                for (int i = 0, n = exprs.Count; i < n; i++)
                {
                    var expr = exprs[i];
                    if (!TryEmit(expr, expr.NodeType, expr.Type, paramExprs, il, ref closure, parent))
                        return false;
                }
                return true;
            }

            private static bool EmitMany(IList<object> exprObjects,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent)
            {
                for (int i = 0, n = exprObjects.Count; i < n; i++)
                {
                    var e = exprObjects[i].GetExprInfo();
                    if (!TryEmit(e.Expr, e.NodeType, e.Type, paramExprs, il, ref closure, parent))
                        return false;
                }
                return true;
            }

            private static bool TryEmitConvert(object exprObj, Type targetType,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var e = exprObj.GetOperandExprInfo();
                if (!TryEmit(e.Expr, e.NodeType, e.Type, paramExprs, il, ref closure, ExpressionType.Convert))
                    return false;

                var sourceType = e.Type;
                if (targetType == sourceType)
                    return true; // do nothing, no conversion needed

                if (targetType == typeof(object))
                {
                    // for value type to object, just box a value, otherwise do nothing - everything is object anyway
                    if (sourceType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Box, sourceType);
                    return true;
                }

                // check implicit / explicit conversion operators on source and target types - #73
                var sourceTypeInfo = sourceType.GetTypeInfo();
                if (!sourceTypeInfo.IsPrimitive)
                {
                    var convertOpMethod = FirstConvertOperatorOrDefault(sourceTypeInfo, targetType, sourceType);
                    if (convertOpMethod != null)
                        return EmitMethodCall(il, convertOpMethod);
                }

                var targetTypeInfo = targetType.GetTypeInfo();
                if (!targetTypeInfo.IsPrimitive)
                {
                    var convertOpMethod = FirstConvertOperatorOrDefault(targetTypeInfo, targetType, sourceType);
                    if (convertOpMethod != null)
                        return EmitMethodCall(il, convertOpMethod);
                }

                if (sourceType == typeof(object) && targetTypeInfo.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, targetType);
                
                // Conversion to Nullable: new Nullable<T>(T val);
                else if (targetTypeInfo.IsGenericType && targetTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
                    il.Emit(OpCodes.Newobj, targetType.GetConstructorByArgs(targetTypeInfo.GenericTypeArguments[0]));
                
                else if (targetType == typeof(int))
                    il.Emit(OpCodes.Conv_I4);
                else if (targetType == typeof(float))
                    il.Emit(OpCodes.Conv_R4);
                else if (targetType == typeof(uint))
                    il.Emit(OpCodes.Conv_U4);
                else if (targetType == typeof(sbyte))
                    il.Emit(OpCodes.Conv_I1);
                else if (targetType == typeof(byte))
                    il.Emit(OpCodes.Conv_U1);
                else if (targetType == typeof(short))
                    il.Emit(OpCodes.Conv_I2);
                else if (targetType == typeof(ushort))
                    il.Emit(OpCodes.Conv_U2);
                else if (targetType == typeof(long))
                    il.Emit(OpCodes.Conv_I8);
                else if (targetType == typeof(ulong))
                    il.Emit(OpCodes.Conv_U8);
                else if (targetType == typeof(double))
                    il.Emit(OpCodes.Conv_R8);

                else // cast as the last resort and let's it fail if unlucky
                    il.Emit(OpCodes.Castclass, targetType);
                return true;
            }

            private static MethodInfo FirstConvertOperatorOrDefault(TypeInfo typeInfo, Type targetType, Type sourceType) =>
                typeInfo.DeclaredMethods.GetFirst(m =>
                    m.IsStatic && m.ReturnType == targetType && 
                    (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                    m.GetParameters()[0].ParameterType == sourceType);

            private static bool TryEmitConstant(object exprObj, Type exprType, ILGenerator il, ref ClosureInfo closure)
            {
                var constExprInfo = exprObj as ConstantExpressionInfo;
                var constantValue = constExprInfo != null ? constExprInfo.Value : ((ConstantExpression)exprObj).Value;
                if (constantValue == null)
                {
                    if (exprType.GetTypeInfo().IsValueType) // handles the conversion of null to Nullable<T>
                        il.Emit(OpCodes.Ldloc, InitValueTypeVariable(il, exprType));
                    else
                        il.Emit(OpCodes.Ldnull);
                    return true;
                }

                var constantType = constantValue.GetType();
                if (IsClosureBoundConstant(constantValue, constantType.GetTypeInfo()))
                {
                    var constantIndex = closure.Constants.GetFirstIndex(exprObj);
                    if (constantIndex == -1)
                        return false;
                    LoadClosureFieldOrItem(ref closure, il, constantIndex, exprType);
                }
                else
                {
                    // get raw enum type to light
                    if (constantType.GetTypeInfo().IsEnum)
                        constantType = Enum.GetUnderlyingType(constantType);

                    if (constantType == typeof(int))
                    {
                        EmitLoadConstantInt(il, (int)constantValue);
                    }
                    else if (constantType == typeof(char))
                    {
                        EmitLoadConstantInt(il, (char)constantValue);
                    }
                    else if (constantType == typeof(short))
                    {
                        EmitLoadConstantInt(il, (short)constantValue);
                    }
                    else if (constantType == typeof(byte))
                    {
                        EmitLoadConstantInt(il, (byte)constantValue);
                    }
                    else if (constantType == typeof(ushort))
                    {
                        EmitLoadConstantInt(il, (ushort)constantValue);
                    }
                    else if (constantType == typeof(sbyte))
                    {
                        EmitLoadConstantInt(il, (sbyte)constantValue);
                    }
                    else if (constantType == typeof(uint))
                    {
                        unchecked
                        {
                            EmitLoadConstantInt(il, (int)(uint)constantValue);
                        }
                    }
                    else if (constantType == typeof(long))
                    {
                        il.Emit(OpCodes.Ldc_I8, (long)constantValue);
                    }
                    else if (constantType == typeof(ulong))
                    {
                        unchecked
                        {
                            il.Emit(OpCodes.Ldc_I8, (long)(ulong)constantValue);
                        }
                    }
                    else if (constantType == typeof(float))
                    {
                        il.Emit(OpCodes.Ldc_R4, (float)constantValue);
                    }
                    else if (constantType == typeof(double))
                    {
                        il.Emit(OpCodes.Ldc_R8, (double)constantValue);
                    }
                    else if (constantType == typeof(bool))
                    {
                        il.Emit((bool)constantValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    }
                    else if (constantValue is string)
                    {
                        il.Emit(OpCodes.Ldstr, (string)constantValue);
                    }
                    else if (constantValue is Type)
                    {
                        il.Emit(OpCodes.Ldtoken, (Type)constantValue);
                        il.Emit(OpCodes.Call, _getTypeFromHandleMethod);
                    }
                    else return false;
                }

                // todo: consider how to remove boxing where it is not required
                // boxing the value type, otherwise we can get a strange result when 0 is treated as Null.
                if (exprType == typeof(object) && constantType.GetTypeInfo().IsValueType)
                    il.Emit(OpCodes.Box, constantValue.GetType()); // using normal type for Enum instead of underlying type

                return true;
            }

            private static LocalBuilder InitValueTypeVariable(ILGenerator il, Type exprType, LocalBuilder existingVar = null)
            {
                var valVar = existingVar ?? il.DeclareLocal(exprType);
                il.Emit(OpCodes.Ldloca, valVar);
                il.Emit(OpCodes.Initobj, exprType);
                return valVar;
            }

            private static void LoadClosureFieldOrItem(ref ClosureInfo closure, ILGenerator il, int itemIndex, 
                Type itemType, object itemExprObj = null)
            {
                il.Emit(OpCodes.Ldarg_0); // closure is always a first argument

                var closureFields = closure.ClosureFields;
                if (closureFields != null)
                    il.Emit(OpCodes.Ldfld, closureFields[itemIndex]);
                else
                {
                    // for ArrayClosure load an array field
                    il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField);

                    // load array item index
                    EmitLoadConstantInt(il, itemIndex);

                    // load item from index
                    il.Emit(OpCodes.Ldelem_Ref);
                    itemType = itemType ?? itemExprObj.GetResultType();
                    il.Emit(itemType.GetTypeInfo().IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, itemType);
                }
            }

            // todo: Replace resultValueVar with a closureInfo block
            private static bool TryEmitNew(object exprObj, Type exprType,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure, LocalBuilder resultValueVar = null)
            {
                ConstructorInfo ctor;
                var exprInfo = exprObj as NewExpressionInfo;
                if (exprInfo != null)
                {
                    if (!EmitMany(exprInfo.Arguments, paramExprs, il, ref closure, ExpressionType.New))
                        return false;
                    ctor = exprInfo.Constructor;
                }
                else
                {
                    var expr = (NewExpression)exprObj;
                    if (!EmitMany(expr.Arguments, paramExprs, il, ref closure, ExpressionType.New))
                        return false;
                    ctor = expr.Constructor;
                }

                if (ctor != null)
                    il.Emit(OpCodes.Newobj, ctor);
                else
                {
                    if (!exprType.GetTypeInfo().IsValueType)
                        return false; // null constructor and not a value type, better fallback

                    var valueVar = InitValueTypeVariable(il, exprType, resultValueVar);
                    if (resultValueVar == null)
                        il.Emit(OpCodes.Ldloc, valueVar);
                }

                return true;
            }

            private static bool EmitNewArray(object exprObj, Type arrayType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var exprInfo = exprObj as NewArrayExpressionInfo;
                if (exprInfo != null)
                    return EmitNewArrayInfo(exprInfo, arrayType, paramExprs, il, ref closure);

                var expr = (NewArrayExpression)exprObj;
                var elems = expr.Expressions;
                var elemType = arrayType.GetElementType();
                if (elemType == null)
                    return false;

                var arrVar = il.DeclareLocal(arrayType);

                var rank = arrayType.GetArrayRank();
                if (rank == 1) // one dimensional
                {
                    EmitLoadConstantInt(il, elems.Count);
                }
                else // multi dimensional
                {
                    var boundsLength = elems.Count;
                    for (var i = 0; i < boundsLength; i++)
                    {
                        var bound = elems[i];
                        if (!TryEmit(bound, bound.NodeType, bound.Type, paramExprs, il, ref closure, ExpressionType.NewArrayInit))
                            return false;
                    }

                    var ctor = arrayType.GetTypeInfo().DeclaredConstructors.GetFirst();
                    if (ctor == null) 
                        return false;
                    il.Emit(OpCodes.Newobj, ctor);

                    return true;
                }

                il.Emit(OpCodes.Newarr, elemType);
                il.Emit(OpCodes.Stloc, arrVar);

                var isElemOfValueType = elemType.GetTypeInfo().IsValueType;

                for (int i = 0, n = elems.Count; i < n; i++)
                {
                    il.Emit(OpCodes.Ldloc, arrVar);
                    EmitLoadConstantInt(il, i);

                    // loading element address for later copying of value into it.
                    if (isElemOfValueType)
                        il.Emit(OpCodes.Ldelema, elemType);

                    var elemExpr = elems[i];
                    if (!TryEmit(elemExpr, elemExpr.NodeType, elemExpr.Type, paramExprs, il, ref closure, ExpressionType.NewArrayInit))
                        return false;

                    if (isElemOfValueType)
                        il.Emit(OpCodes.Stobj, elemType); // store element of value type by array element address
                    else
                        il.Emit(OpCodes.Stelem_Ref);
                }

                il.Emit(OpCodes.Ldloc, arrVar);
                return true;
            }

            private static bool EmitNewArrayInfo(NewArrayExpressionInfo expr, Type arrayType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var elemExprObjects = expr.Arguments;
                var elemType = arrayType.GetElementType();
                if (elemType == null)
                    return false;

                var isElemOfValueType = elemType.GetTypeInfo().IsValueType;

                var arrVar = il.DeclareLocal(arrayType);

                EmitLoadConstantInt(il, elemExprObjects.Length);
                il.Emit(OpCodes.Newarr, elemType);
                il.Emit(OpCodes.Stloc, arrVar);

                for (var i = 0; i < elemExprObjects.Length; i++)
                {
                    il.Emit(OpCodes.Ldloc, arrVar);
                    EmitLoadConstantInt(il, i);

                    // loading element address for later copying of value into it.
                    if (isElemOfValueType)
                        il.Emit(OpCodes.Ldelema, elemType);

                    var e = elemExprObjects[i].GetExprInfo();
                    if (!TryEmit(e.Expr, e.NodeType, e.Type, paramExprs, il, ref closure, ExpressionType.NewArrayInit))
                        return false;

                    if (isElemOfValueType)
                        il.Emit(OpCodes.Stobj, elemType); // store element of value type by array element address
                    else
                        il.Emit(OpCodes.Stelem_Ref);
                }

                il.Emit(OpCodes.Ldloc, arrVar);
                return true;
            }

            private static bool TryEmitArrayIndex(object exprObj, Type exprType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                if (!EmitBinary(exprObj, paramExprs, il, ref closure, ExpressionType.ArrayIndex))
                    return false;
                if (exprType.GetTypeInfo().IsValueType)
                    il.Emit(OpCodes.Ldelem, exprType);
                else
                    il.Emit(OpCodes.Ldelem_Ref);
                return true;
            }

            private static bool EmitMemberInit(object exprObj, Type exprType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType stack)
            {
                var exprInfo = exprObj as MemberInitExpressionInfo;
                if (exprInfo != null)
                    return EmitMemberInitInfo(exprInfo, exprType, paramExprs, il, ref closure, stack);

                // todo: Use closureInfo Block to track the variable instead
                LocalBuilder valueVar = null;
                if (exprType.GetTypeInfo().IsValueType)
                    valueVar = il.DeclareLocal(exprType);

                var expr = (MemberInitExpression)exprObj;
                if (!TryEmitNew(expr.NewExpression, exprType, paramExprs, il, ref closure, valueVar))
                    return false;

                var bindings = expr.Bindings;
                for (var i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    if (binding.BindingType != MemberBindingType.Assignment)
                        return false;

                    if (valueVar != null) // load local value address, to set its members
                        il.Emit(OpCodes.Ldloca, valueVar);
                    else
                        il.Emit(OpCodes.Dup); // duplicate member owner on stack

                    var bindingExpr = ((MemberAssignment)binding).Expression;
                    if (!TryEmit(bindingExpr, bindingExpr.NodeType, bindingExpr.Type, paramExprs, il, ref closure, stack) ||
                        !EmitMemberAssign(il, binding.Member))
                        return false;
                }

                if (valueVar != null)
                    il.Emit(OpCodes.Ldloc, valueVar);

                return true;
            }

            private static bool EmitMemberInitInfo(MemberInitExpressionInfo exprInfo, Type exprType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType stack)
            {
                LocalBuilder valueVar = null;
                if (exprType.GetTypeInfo().IsValueType)
                    valueVar = il.DeclareLocal(exprType);

                var objInfo = exprInfo.ExpressionInfo;
                if (objInfo == null)
                    return false; // static initialization is Not supported

                var newExpr = exprInfo.NewExpressionInfo;
                if (newExpr != null)
                {
                    if (!TryEmitNew(newExpr, exprType, paramExprs, il, ref closure, valueVar))
                        return false;
                }
                else
                {
                    if (!TryEmit(objInfo, objInfo.NodeType, objInfo.Type, paramExprs, il, ref closure, stack))
                        return false;
                }

                var bindings = exprInfo.Bindings;
                for (var i = 0; i < bindings.Length; i++)
                {
                    var binding = bindings[i];

                    if (valueVar != null) // load local value address, to set its members
                        il.Emit(OpCodes.Ldloca, valueVar);
                    else
                        il.Emit(OpCodes.Dup); // duplicate member owner on stack

                    var bindingExpr = binding.Expression;
                    if (!TryEmit(bindingExpr, bindingExpr.NodeType, bindingExpr.Type, paramExprs, il, ref closure, stack) ||
                        !EmitMemberAssign(il, binding.Member))
                        return false;
                }

                if (valueVar != null)
                    il.Emit(OpCodes.Ldloc, valueVar);

                return true;
            }

            private static bool EmitMemberAssign(ILGenerator il, MemberInfo member)
            {
                var prop = member as PropertyInfo;
                if (prop != null)
                {
                    var setMethod = prop.DeclaringType.GetTypeInfo().GetDeclaredMethod("set_" + prop.Name);
                    return setMethod != null && EmitMethodCall(il, setMethod);
                }

                var field = member as FieldInfo;
                if (field == null)
                    return false;
                il.Emit(OpCodes.Stfld, field);
                return true;
            }

            private static bool TryEmitAssign(object exprObj, Type exprType,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                object left, right;
                ExpressionType leftNodeType, rightNodeType;

                var expr = exprObj as BinaryExpression;
                if (expr != null)
                {
                    left = expr.Left;
                    right = expr.Right;
                    leftNodeType = expr.Left.NodeType;
                    rightNodeType = expr.Right.NodeType;
                }
                else
                {
                    var info = (BinaryExpressionInfo)exprObj;
                    left = info.Left;
                    right = info.Right;
                    leftNodeType = left.GetNodeType();
                    rightNodeType = right.GetNodeType();
                }

                // if this assignment is part of a single body-less expression or the result of a block
                // we should put its result to the evaluation stack before the return, otherwise we are
                // somewhere inside the block, so we shouldn't return with the result
                var shouldPushResult = closure.CurrentBlock.IsEmpty || closure.CurrentBlock.ResultExpr == exprObj;

                switch (leftNodeType)
                {
                    case ExpressionType.Parameter:
                        var paramIndex = paramExprs.GetFirstIndex(left);
                        if (paramIndex != -1)
                        {
                            if (closure.HasClosure)
                                paramIndex += 1; // shift parameter indices by one, because the first one will be closure

                            if (paramIndex >= byte.MaxValue)
                                return false;

                            if (!TryEmit(right, rightNodeType, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;

                            if (shouldPushResult)
                                il.Emit(OpCodes.Dup); // dup value to assign and return

                            il.Emit(OpCodes.Starg_S, paramIndex);
                            return true;
                        }

                        // if parameter isn't passed, then it is passed into some outer lambda or it is a local variable,
                        // so it should be loaded from closure or from the locals. Then the closure is null will be an invalid state.
                        if (!closure.IsClosureConstructed)
                            return false;

                        // if it's a local variable, then store the right value in it
                        var localVariable = closure.GetDefinedLocalVarOrDefault(left);
                        if (localVariable != null)
                        {
                            if (!TryEmit(right, rightNodeType, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;

                            if (shouldPushResult) // if we have to push the result back, dup the right value
                                il.Emit(OpCodes.Dup);

                            il.Emit(OpCodes.Stloc, localVariable);
                            return true;
                        }

                        // check that it's a captured parameter by closure
                        var nonPassedParamIndex = closure.NonPassedParameters.GetFirstIndex(left);
                        if (nonPassedParamIndex == -1)
                            return false;  // what??? no chance

                        var paramInClosureIndex = closure.Constants.Length + nonPassedParamIndex;

                        il.Emit(OpCodes.Ldarg_0); // closure is always a first argument

                        if (shouldPushResult)
                        {
                            if (!TryEmit(right, rightNodeType, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;

                            var valueVar = il.DeclareLocal(exprType); // store left value in variable
                            if (closure.ClosureFields != null)
                            {
                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Stloc, valueVar);
                                il.Emit(OpCodes.Stfld, closure.ClosureFields[paramInClosureIndex]);
                                il.Emit(OpCodes.Ldloc, valueVar);
                            }
                            else
                            {
                                il.Emit(OpCodes.Stloc, valueVar);
                                il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField); // load array field
                                EmitLoadConstantInt(il, paramInClosureIndex); // load array item index
                                il.Emit(OpCodes.Ldloc, valueVar);
                                if (exprType.GetTypeInfo().IsValueType)
                                    il.Emit(OpCodes.Box, exprType);
                                il.Emit(OpCodes.Stelem_Ref); // put the variable into array
                                il.Emit(OpCodes.Ldloc, valueVar);
                            }
                        }
                        else
                        {
                            var isArrayClosure = closure.ClosureFields == null;
                            if (isArrayClosure)
                            {
                                il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField); // load array field
                                EmitLoadConstantInt(il, paramInClosureIndex); // load array item index
                            }

                            if (!TryEmit(right, rightNodeType, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;

                            if (isArrayClosure)
                            {
                                if (exprType.GetTypeInfo().IsValueType)
                                    il.Emit(OpCodes.Box, exprType);
                                il.Emit(OpCodes.Stelem_Ref); // put the variable into array
                            }
                            else
                                il.Emit(OpCodes.Stfld, closure.ClosureFields[paramInClosureIndex]);
                        }
                        return true;

                    case ExpressionType.MemberAccess:
                        object objExpr;
                        MemberInfo member;
                        var memberExpr = left as MemberExpression;
                        if (memberExpr != null)
                        {
                            objExpr = memberExpr.Expression;
                            member = memberExpr.Member;
                        }
                        else
                        {
                            var memberExprInfo = (MemberExpressionInfo)left;
                            objExpr = memberExprInfo.Expression;
                            member = memberExprInfo.Member;
                        }

                        if (objExpr != null)
                        {
                            var e = objExpr.GetExprInfo();
                            if (!TryEmit(e.Expr, e.NodeType, e.Type, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;
                        }

                        if (!TryEmit(right, rightNodeType, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                            return false;

                        if (!shouldPushResult)
                            return EmitMemberAssign(il, member);

                        il.Emit(OpCodes.Dup);

                        var rightVar = il.DeclareLocal(exprType); // store right value in variable
                        il.Emit(OpCodes.Stloc, rightVar);

                        if (!EmitMemberAssign(il, member))
                            return false;

                        il.Emit(OpCodes.Ldloc, rightVar);
                        return true;

                    case ExpressionType.Index:
                        var indexExpr = (IndexExpression)left; // todo: add IndexExpressionInfo

                        var obj = indexExpr.Object;
                        if (obj != null && !TryEmit(obj, obj.NodeType, obj.Type, paramExprs, il, ref closure, ExpressionType.Assign))
                            return false;

                        var argLength = indexExpr.Arguments.Count;
                        for (var i = 0; i < argLength; i++)
                        {
                            var arg = indexExpr.Arguments[i];
                            if (!TryEmit(arg, arg.NodeType, arg.Type, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;
                        }

                        if (!TryEmit(right, rightNodeType, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                            return false;

                        if (!shouldPushResult)
                            return TryEmitIndexAssign(indexExpr, obj?.Type, exprType, il);

                        var variable = il.DeclareLocal(exprType); // store value in variable to return
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc, variable);

                        if (!TryEmitIndexAssign(indexExpr, obj?.Type, exprType, il))
                            return false;

                        il.Emit(OpCodes.Ldloc, variable);
                        return true;

                    default: // not yet support assignment targets
                        return false;
                }
            }

            private static bool TryEmitIndexAssign(IndexExpression indexExpr, Type instType, Type elementType, ILGenerator il)
            {
                if (indexExpr.Indexer != null)
                    return EmitMemberAssign(il, indexExpr.Indexer);

                if (indexExpr.Arguments.Count == 1) // one dimensional array
                {
                    if (elementType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Stelem, elementType);
                    else
                        il.Emit(OpCodes.Stelem_Ref);
                    return true;
                }

                // multi dimensional array
                var setMethod = instType?.GetTypeInfo().GetDeclaredMethod("Set");
                return setMethod != null && EmitMethodCall(il, setMethod);
            }

            private static bool TryEmitMethodCall(object exprObj, object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var isValueTypeObj = false;
                Type objType = null;
                var exprInfo = exprObj as MethodCallExpressionInfo;
                if (exprInfo != null)
                {
                    var objExpr = exprInfo.Object;
                    if (objExpr != null)
                    {
                        objType = objExpr.Type;
                        if (!TryEmit(objExpr, objExpr.NodeType, objType, paramExprs, il, ref closure, ExpressionType.Call))
                            return false;

                        isValueTypeObj = objType.GetTypeInfo().IsValueType;
                        if (isValueTypeObj && objExpr.NodeType != ExpressionType.Parameter)
                            StoreAsVarAndLoadItsAddress(il, objType);
                    }

                    if (exprInfo.Arguments.Length != 0 &&
                        !EmitMany(exprInfo.Arguments, paramExprs, il, ref closure, ExpressionType.Call))
                        return false;
                }
                else
                {
                    var expr = (MethodCallExpression)exprObj;
                    var objExpr = expr.Object;
                    if (objExpr != null)
                    {
                        objType = objExpr.Type;
                        if (!TryEmit(objExpr, objExpr.NodeType, objType, paramExprs, il, ref closure, ExpressionType.Call))
                            return false;

                        isValueTypeObj = objType.GetTypeInfo().IsValueType;
                        if (isValueTypeObj && objExpr.NodeType != ExpressionType.Parameter)
                            StoreAsVarAndLoadItsAddress(il, objType);
                    }

                    if (expr.Arguments.Count != 0 && 
                        !EmitMany(expr.Arguments, paramExprs, il, ref closure, ExpressionType.Call))
                        return false;
                }

                var method = exprInfo != null ? exprInfo.Method : ((MethodCallExpression)exprObj).Method;
                if (isValueTypeObj && method.IsVirtual)
                    il.Emit(OpCodes.Constrained, objType);

                return EmitMethodCall(il, method);
            }

            private static void StoreAsVarAndLoadItsAddress(ILGenerator il, Type varType)
            {
                var theVar = il.DeclareLocal(varType);
                il.Emit(OpCodes.Stloc, theVar);
                il.Emit(OpCodes.Ldloca, theVar);
            }

            private static bool TryEmitMemberAccess(object exprObj, object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                MemberInfo member;
                var objType = default(Type);
                var objNodeType = ExpressionType.Default;
                PropertyInfo prop;
                var exprInfo = exprObj as MemberExpressionInfo;
                if (exprInfo != null)
                {
                    member = exprInfo.Member;
                    prop = member as PropertyInfo;

                    var objExpr = exprInfo.Expression;
                    if (objExpr != null)
                    {
                        var e = objExpr.GetExprInfo();
                        objType = e.Type;
                        objNodeType = e.NodeType;
                        if (!TryEmit(e.Expr, e.NodeType, e.Type, paramExprs, il, ref closure,
                            prop != null ? ExpressionType.Call : ExpressionType.MemberAccess))
                            return false;
                    }
                }
                else
                {
                    var expr = (MemberExpression)exprObj;
                    member = expr.Member;
                    prop = member as PropertyInfo;

                    var objExpr = expr.Expression;
                    if (objExpr != null)
                    {
                        objType = objExpr.Type;
                        objNodeType = objExpr.NodeType;
                        if (!TryEmit(objExpr, objNodeType, objType, paramExprs, il, ref closure,
                            prop != null ? ExpressionType.Call : ExpressionType.MemberAccess))
                            return false;
                    }
                }

                // Value type special treatment to load address of value instance in order to access a field or call a method.
                // Parameter should be excluded because it already loads an address via Ldarga, and you don't need to.
                // And for field access no need to load address, cause the field stored on stack nearby
                if (objType != null && objNodeType != ExpressionType.Parameter && prop != null && 
                    objType.GetTypeInfo().IsValueType)
                    StoreAsVarAndLoadItsAddress(il, objType);

                if (prop != null)
                {
                    var propGetMethod = TryGetPropertyGetMethod(prop);
                    return propGetMethod != null && EmitMethodCall(il, propGetMethod);
                }

                var field = member as FieldInfo;
                if (field != null)
                {
                    il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                    return true;
                }

                return false;
            }

            private static MethodInfo TryGetPropertyGetMethod(PropertyInfo prop) => 
                prop.DeclaringType.GetTypeInfo().GetDeclaredMethod("get_" + prop.Name);

            private static bool TryEmitNestedLambda(object lambdaExpr,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                // First, find in closed compiled lambdas the one corresponding to the current lambda expression.
                // Situation with not found lambda is not possible/exceptional,
                // it means that we somehow skipped the lambda expression while collecting closure info.
                var outerNestedLambdas = closure.NestedLambdas;
                var outerNestedLambdaIndex = outerNestedLambdas.GetFirstIndex(it => it.LambdaExpr == lambdaExpr);
                if (outerNestedLambdaIndex == -1)
                    return false;

                var nestedLambdaInfo = outerNestedLambdas[outerNestedLambdaIndex];
                var nestedLambda = nestedLambdaInfo.Lambda;

                var outerConstants = closure.Constants;
                var outerNonPassedParams = closure.NonPassedParameters;

                // Load compiled lambda on stack counting the offset
                outerNestedLambdaIndex += outerConstants.Length + outerNonPassedParams.Length;

                LoadClosureFieldOrItem(ref closure, il, outerNestedLambdaIndex, nestedLambda.GetType());

                // If lambda does not use any outer parameters to be set in closure, then we're done
                var nestedClosureInfo = nestedLambdaInfo.ClosureInfo;
                if (!nestedClosureInfo.HasClosure)
                    return true;

                // If closure is array-based, the create a new array to represent closure for the nested lambda
                var isNestedArrayClosure = nestedClosureInfo.ClosureFields == null;
                if (isNestedArrayClosure)
                {
                    EmitLoadConstantInt(il, nestedClosureInfo.ClosedItemCount); // size of array
                    il.Emit(OpCodes.Newarr, typeof(object));
                }

                // Load constants on stack
                var nestedConstants = nestedClosureInfo.Constants;
                if (nestedConstants.Length != 0)
                {
                    for (var nestedConstIndex = 0; nestedConstIndex < nestedConstants.Length; nestedConstIndex++)
                    {
                        var nestedConstant = nestedConstants[nestedConstIndex];

                        // Find constant index in the outer closure
                        var outerConstIndex = outerConstants.GetFirstIndex(nestedConstant);
                        if (outerConstIndex == -1)
                            return false; // some error is here

                        if (isNestedArrayClosure)
                        {
                            // Duplicate nested array on stack to store the item, and load index to where to store
                            il.Emit(OpCodes.Dup);
                            EmitLoadConstantInt(il, nestedConstIndex);
                        }

                        var nestedConstantType = nestedConstant.GetResultType();
                        LoadClosureFieldOrItem(ref closure, il, outerConstIndex, nestedConstantType);

                        if (isNestedArrayClosure)
                        {
                            if (nestedConstantType.GetTypeInfo().IsValueType)
                                il.Emit(OpCodes.Box, nestedConstantType);
                            il.Emit(OpCodes.Stelem_Ref); // store the item in array
                        }
                    }
                }

                // Load used and closed parameter values on stack
                var nestedNonPassedParams = nestedClosureInfo.NonPassedParameters;
                for (var nestedParamIndex = 0; nestedParamIndex < nestedNonPassedParams.Length; nestedParamIndex++)
                {
                    var nestedUsedParam = nestedNonPassedParams[nestedParamIndex];

                    Type nestedUsedParamType = null;
                    if (isNestedArrayClosure)
                    {
                        // get a param type for the later
                        nestedUsedParamType = nestedUsedParam.GetResultType();

                        // Duplicate nested array on stack to store the item, and load index to where to store
                        il.Emit(OpCodes.Dup);
                        EmitLoadConstantInt(il, nestedConstants.Length + nestedParamIndex);
                    }

                    var paramIndex = paramExprs.GetFirstIndex(nestedUsedParam);
                    if (paramIndex != -1) // load param from input params
                    {
                        // +1 is set cause of added first closure argument
                        LoadParamArg(il, 1 + paramIndex, false);
                    }
                    else // load parameter from outer closure or from the locals
                    {
                        if (outerNonPassedParams.Length == 0)
                            return false; // impossible, better to throw?

                        var variable = closure.GetDefinedLocalVarOrDefault(nestedUsedParam);
                        if (variable != null) // it's a local variable
                        {
                            il.Emit(OpCodes.Ldloc, variable);
                        }
                        else // it's a parameter from outer closure
                        {
                            var outerParamIndex = outerNonPassedParams.GetFirstIndex(nestedUsedParam);
                            if (outerParamIndex == -1)
                                return false; // impossible, better to throw?

                            LoadClosureFieldOrItem(ref closure, il, outerConstants.Length + outerParamIndex, 
                                nestedUsedParamType, nestedUsedParam);
                        }
                    }

                    if (isNestedArrayClosure)
                    {
                        if (nestedUsedParamType.GetTypeInfo().IsValueType)
                            il.Emit(OpCodes.Box, nestedUsedParamType);

                        il.Emit(OpCodes.Stelem_Ref); // store the item in array
                    }
                }

                // Load nested lambdas on stack
                var nestedNestedLambdas = nestedClosureInfo.NestedLambdas;
                if (nestedNestedLambdas.Length != 0)
                {
                    for (var nestedLambdaIndex = 0; nestedLambdaIndex < nestedNestedLambdas.Length; nestedLambdaIndex++)
                    {
                        var nestedNestedLambda = nestedNestedLambdas[nestedLambdaIndex];

                        // Find constant index in the outer closure
                        var outerLambdaIndex = outerNestedLambdas.GetFirstIndex(it => it.LambdaExpr == nestedNestedLambda.LambdaExpr);
                        if (outerLambdaIndex == -1)
                            return false; // some error is here

                        // Duplicate nested array on stack to store the item, and load index to where to store
                        if (isNestedArrayClosure)
                        {
                            il.Emit(OpCodes.Dup);
                            EmitLoadConstantInt(il, nestedConstants.Length + nestedNonPassedParams.Length + nestedLambdaIndex);
                        }

                        outerLambdaIndex += outerConstants.Length + outerNonPassedParams.Length;

                        LoadClosureFieldOrItem(ref closure, il, outerLambdaIndex, nestedNestedLambda.Lambda.GetType());

                        if (isNestedArrayClosure)
                            il.Emit(OpCodes.Stelem_Ref); // store the item in array
                    }
                }

                // Create nested closure object composed of all constants, params, lambdas loaded on stack
                if (isNestedArrayClosure)
                    il.Emit(OpCodes.Newobj, ArrayClosure.Constructor);
                else
                    il.Emit(OpCodes.Newobj, nestedClosureInfo.ClosureType.GetTypeInfo().DeclaredConstructors.GetFirst());

                return EmitMethodCall(il, GetCurryClosureMethod(nestedLambda, nestedLambdaInfo.IsAction));
            }

            private static MethodInfo GetCurryClosureMethod(object lambda, bool isAction)
            {
                var lambdaTypeArgs = lambda.GetType().GetTypeInfo().GenericTypeArguments;
                return isAction
                    ? CurryClosureActions.Methods[lambdaTypeArgs.Length - 1].MakeGenericMethod(lambdaTypeArgs)
                    : CurryClosureFuncs.Methods[lambdaTypeArgs.Length - 2].MakeGenericMethod(lambdaTypeArgs);
            }

            private static bool TryInvokeLambda(object exprObj,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var expr = exprObj as InvocationExpression;
                Type lambdaType;
                if (expr != null)
                {
                    var lambdaExpr = expr.Expression;
                    lambdaType = lambdaExpr.Type;
                    if (!TryEmit(lambdaExpr, lambdaExpr.NodeType, lambdaType, paramExprs, il, ref closure, ExpressionType.Invoke) ||
                        !EmitMany(expr.Arguments, paramExprs, il, ref closure, ExpressionType.Invoke))
                        return false;
                }
                else
                {
                    var exprInfo = (InvocationExpressionInfo)exprObj;
                    var lambdaExprInfo = exprInfo.ExprToInvoke;
                    lambdaType = lambdaExprInfo.Type;
                    if (!TryEmit(lambdaExprInfo, lambdaExprInfo.NodeType, lambdaType, paramExprs, il, ref closure, ExpressionType.Invoke) ||
                        !EmitMany(exprInfo.Arguments, paramExprs, il, ref closure, ExpressionType.Invoke))
                        return false;
                }

                var invokeMethod = lambdaType.GetTypeInfo().GetDeclaredMethod("Invoke");
                return invokeMethod != null && EmitMethodCall(il, invokeMethod);
            }

            private static bool TryEmitComparison(object exprObj, ExpressionType exprNodeType, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                if (!EmitBinary(exprObj, paramExprs, il, ref closure, ExpressionType.Default))
                    return false;

                // todo: for now, handling only parameters of the same type
                // todo: for now, Nullable is not supported
                Type leftOpType, rightOpType;
                var expr = exprObj as BinaryExpression;
                if (expr != null)
                {
                    leftOpType = expr.Left.Type;
                    rightOpType = expr.Right.Type;
                }
                else
                {
                    var exprInfo = (BinaryExpressionInfo)exprObj;
                    leftOpType = exprInfo.Left.GetResultType();
                    rightOpType = exprInfo.Right.GetResultType();
                }

                if (leftOpType != rightOpType || leftOpType.IsNullable())
                    return false;

                var leftOpTypeInfo = leftOpType.GetTypeInfo();
                if (!leftOpTypeInfo.IsPrimitive && !leftOpTypeInfo.IsEnum)
                {
                    var methodName
                        = exprNodeType == ExpressionType.Equal ? "op_Equality"
                        : exprNodeType == ExpressionType.NotEqual ? "op_Inequality"
                        : exprNodeType == ExpressionType.GreaterThan ? "op_GreaterThan"
                        : exprNodeType == ExpressionType.GreaterThanOrEqual ? "op_GreaterThanOrEqual"
                        : exprNodeType == ExpressionType.LessThan ? "op_LessThan"
                        : exprNodeType == ExpressionType.LessThanOrEqual ? "op_LessThanOrEqual" : null;

                    if (methodName == null)
                        return false;

                    // todo: for now handling only parameters of the same type
                    var method = leftOpTypeInfo.DeclaredMethods.GetFirst(m =>
                        m.IsStatic && m.Name == methodName &&
                        m.GetParameters().All(p => p.ParameterType == leftOpType));

                    if (method != null)
                        return EmitMethodCall(il, method);
                    
                    if (exprNodeType != ExpressionType.Equal && exprNodeType != ExpressionType.NotEqual)
                        return false;

                    EmitMethodCall(il, _objectEqualsMethod);
                    if (exprNodeType == ExpressionType.NotEqual) // invert result for not equal
                    {
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                    }

                    return true;
                }

                // handle primitives comparison
                switch (exprNodeType)
                {
                    case ExpressionType.Equal:
                        il.Emit(OpCodes.Ceq);
                        return true;

                    case ExpressionType.NotEqual:
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        return true;

                    case ExpressionType.LessThan:
                        il.Emit(OpCodes.Clt);
                        return true;

                    case ExpressionType.GreaterThan:
                        il.Emit(OpCodes.Cgt);
                        return true;

                    case ExpressionType.LessThanOrEqual:
                        il.Emit(OpCodes.Cgt);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        return true;

                    case ExpressionType.GreaterThanOrEqual:
                        il.Emit(OpCodes.Clt);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        return true;
                }
                return false;
            }

            private static bool TryEmitArithmeticOperation(object exprObj, Type exprType, ExpressionType exprNodeType,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                if (!EmitBinary(exprObj, paramExprs, il, ref closure, ExpressionType.Default))
                    return false;

                var exprTypeInfo = exprType.GetTypeInfo();
                if (!exprTypeInfo.IsPrimitive)
                {
                    var methodName
                        = exprNodeType == ExpressionType.Add ? "op_Addition"
                        : exprNodeType == ExpressionType.AddChecked ? "op_Addition"
                        : exprNodeType == ExpressionType.Subtract ? "op_Subtraction"
                        : exprNodeType == ExpressionType.SubtractChecked ? "op_Subtraction"
                        : exprNodeType == ExpressionType.Multiply ? "op_Multiply"
                        : exprNodeType == ExpressionType.Divide ? "op_Division"
                        : null;

                    var method = methodName != null ? exprTypeInfo.GetDeclaredMethod(methodName) : null;
                    return method != null && EmitMethodCall(il, method);
                }

                switch (exprNodeType)
                {
                    case ExpressionType.Add:
                        il.Emit(OpCodes.Add);
                        return true;

                    case ExpressionType.AddChecked:
                        il.Emit(IsUnsigned(exprType) ? OpCodes.Add_Ovf_Un : OpCodes.Add_Ovf);
                        return true;

                    case ExpressionType.Subtract:
                        il.Emit(OpCodes.Sub);
                        return true;

                    case ExpressionType.SubtractChecked:
                        il.Emit(IsUnsigned(exprType) ? OpCodes.Sub_Ovf_Un : OpCodes.Sub_Ovf);
                        return true;

                    case ExpressionType.Multiply:
                        il.Emit(OpCodes.Mul);
                        return true;

                    case ExpressionType.MultiplyChecked:
                        il.Emit(IsUnsigned(exprType) ? OpCodes.Mul_Ovf_Un : OpCodes.Mul_Ovf);
                        return true;

                    case ExpressionType.Divide:
                        il.Emit(OpCodes.Div);
                        return true;
                }

                return false;
            }

            private static bool IsUnsigned(Type type) =>
                type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);

            private static bool TryEmitLogicalOperator(BinaryExpression expr,
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var leftExpr = expr.Left;
                if (!TryEmit(leftExpr, leftExpr.NodeType, leftExpr.Type, paramExprs, il, ref closure, ExpressionType.Default))
                    return false;

                var labelSkipRight = il.DefineLabel();
                var isAnd = expr.NodeType == ExpressionType.AndAlso;
                il.Emit(isAnd ? OpCodes.Brfalse : OpCodes.Brtrue, labelSkipRight);

                var rightExpr = expr.Right;
                if (!TryEmit(rightExpr, rightExpr.NodeType, rightExpr.Type, paramExprs, il, ref closure, ExpressionType.Default))
                    return false;

                var labelDone = il.DefineLabel();
                il.Emit(OpCodes.Br, labelDone);

                il.MarkLabel(labelSkipRight); // label the second branch
                il.Emit(isAnd ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
                il.MarkLabel(labelDone);
                return true;
            }

            private static bool TryEmitConditional(ConditionalExpression expr, 
                object[] paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var testExpr = expr.Test;
                if (!TryEmit(testExpr, testExpr.NodeType, testExpr.Type, paramExprs, il, ref closure, ExpressionType.Conditional))
                    return false;

                var labelIfFalse = il.DefineLabel();
                il.Emit(OpCodes.Brfalse, labelIfFalse);

                var ifTrueExpr = expr.IfTrue;
                if (!TryEmit(ifTrueExpr, ifTrueExpr.NodeType, ifTrueExpr.Type, paramExprs, il, ref closure, ExpressionType.Conditional))
                    return false;

                var labelDone = il.DefineLabel();
                il.Emit(OpCodes.Br, labelDone);

                il.MarkLabel(labelIfFalse);
                var ifFalseExpr = expr.IfFalse;
                if (!TryEmit(ifFalseExpr, ifFalseExpr.NodeType, ifFalseExpr.Type, paramExprs, il, ref closure, ExpressionType.Conditional))
                    return false;

                il.MarkLabel(labelDone);
                return true;
            }

            private static bool EmitMethodCall(ILGenerator il, MethodInfo method)
            {
                il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
                return true;
            }

            private static void EmitLoadConstantInt(ILGenerator il, int i)
            {
                switch (i)
                {
                    case -1:
                        il.Emit(OpCodes.Ldc_I4_M1);
                        break;
                    case 0:
                        il.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldc_I4_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldc_I4_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldc_I4_3);
                        break;
                    case 4:
                        il.Emit(OpCodes.Ldc_I4_4);
                        break;
                    case 5:
                        il.Emit(OpCodes.Ldc_I4_5);
                        break;
                    case 6:
                        il.Emit(OpCodes.Ldc_I4_6);
                        break;
                    case 7:
                        il.Emit(OpCodes.Ldc_I4_7);
                        break;
                    case 8:
                        il.Emit(OpCodes.Ldc_I4_8);
                        break;
                    default:
                        il.Emit(OpCodes.Ldc_I4, i);
                        break;
                }
            }
        }
    }

    struct ExprInfo
    {
        public readonly ExpressionType NodeType;
        public readonly Type Type;
        public readonly object Expr;

        public ExprInfo(object expr, ExpressionType nodeType, Type type) { Expr = expr; NodeType = nodeType; Type = type; }
    }

    // Helpers targeting the performance. Extensions method names may be a bit funny (non standard), 
    // in order to prevent conflicts with YOUR helpers with standard names
    static class Tools
    {
        public static bool IsNullable(this Type type) =>
            type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>);

        public static ConstructorInfo GetConstructorByArgs(this Type type, params Type[] args) =>
            type.GetTypeInfo().DeclaredConstructors.GetFirst(c => c.GetParameters().Project(p => p.ParameterType).SequenceEqual(args));

        public static Expression ToExpression(this object exprObj) =>
            exprObj == null ? null : exprObj as Expression ?? ((ExpressionInfo)exprObj).ToExpression();

        public static ExprInfo GetExprInfo(this object exprObj)
        {
            var expr = exprObj as Expression;
            if (expr != null)
                return new ExprInfo(exprObj, expr.NodeType, expr.Type);
            var exprInfo = (ExpressionInfo)exprObj;
            return new ExprInfo(exprObj, exprInfo.NodeType, exprInfo.Type);
        }

        public static ExprInfo GetOperandExprInfo(this object exprObj) => 
            (exprObj as UnaryExpression)?.Operand.GetExprInfo() ?? ((UnaryExpressionInfo)exprObj).Operand.GetExprInfo();

        public static ExpressionType GetNodeType(this object exprObj) =>
            (exprObj as Expression)?.NodeType ?? ((ExpressionInfo)exprObj).NodeType;

        public static Type GetResultType(this object exprObj) =>
            (exprObj as Expression)?.Type ?? ((ExpressionInfo)exprObj).Type;

        public static T[] AsArray<T>(this IEnumerable<T> xs) => xs as T[] ?? xs.ToArray();

        private static class EmptyArray<T>
        {
            public static readonly T[] Value = new T[0];
        }

        public static T[] Empty<T>() => EmptyArray<T>.Value;

        public static T[] WithLast<T>(this T[] source, T value)
        {
            if (source == null || source.Length == 0)
                return new[] { value };
            if (source.Length == 1)
                return new[] { source[0], value };
            if (source.Length == 2)
                return new[] { source[0], source[1], value };
            var sourceLength = source.Length;
            var result = new T[sourceLength + 1];
            Array.Copy(source, result, sourceLength);
            result[sourceLength] = value;
            return result;
        }

        public static Type[] GetParamExprTypes(IList<ParameterExpression> paramExprs)
        {
            if (paramExprs == null || paramExprs.Count == 0)
                return Empty<Type>();

            if (paramExprs.Count == 1)
                return new[] { paramExprs[0].IsByRef ? paramExprs[0].Type.MakeByRefType() : paramExprs[0].Type };

            var paramTypes = new Type[paramExprs.Count];
            for (var i = 0; i < paramTypes.Length; i++)
                paramTypes[i] = paramExprs[i].IsByRef ? paramExprs[i].Type.MakeByRefType() : paramExprs[i].Type;

            return paramTypes;
        }

        // todo: Add ByRef handling
        public static Type[] GetParamExprTypes(IList<object> paramExprs)
        {
            if (paramExprs == null || paramExprs.Count == 0)
                return Empty<Type>();

            if (paramExprs.Count == 1)
                return new[] { paramExprs[0].GetResultType() };

            var paramTypes = new Type[paramExprs.Count];
            for (var i = 0; i < paramTypes.Length; i++)
                paramTypes[i] = paramExprs[i].GetResultType();
            return paramTypes;
        }

        public static Type GetFuncOrActionType(Type[] paramTypes, Type returnType)
        {
            if (returnType == typeof(void))
            {
                switch (paramTypes.Length)
                {
                    case 0: return typeof(Action);
                    case 1: return typeof(Action<>).MakeGenericType(paramTypes);
                    case 2: return typeof(Action<,>).MakeGenericType(paramTypes);
                    case 3: return typeof(Action<,,>).MakeGenericType(paramTypes);
                    case 4: return typeof(Action<,,,>).MakeGenericType(paramTypes);
                    case 5: return typeof(Action<,,,,>).MakeGenericType(paramTypes);
                    case 6: return typeof(Action<,,,,,>).MakeGenericType(paramTypes);
                    case 7: return typeof(Action<,,,,,,>).MakeGenericType(paramTypes);
                    default:
                        throw new NotSupportedException(
                            string.Format("Action with so many ({0}) parameters is not supported!", paramTypes.Length));
                }
            }

            paramTypes = paramTypes.WithLast(returnType);
            switch (paramTypes.Length)
            {
                case 1: return typeof(Func<>).MakeGenericType(paramTypes);
                case 2: return typeof(Func<,>).MakeGenericType(paramTypes);
                case 3: return typeof(Func<,,>).MakeGenericType(paramTypes);
                case 4: return typeof(Func<,,,>).MakeGenericType(paramTypes);
                case 5: return typeof(Func<,,,,>).MakeGenericType(paramTypes);
                case 6: return typeof(Func<,,,,,>).MakeGenericType(paramTypes);
                case 7: return typeof(Func<,,,,,,>).MakeGenericType(paramTypes);
                case 8: return typeof(Func<,,,,,,,>).MakeGenericType(paramTypes);
                default:
                    throw new NotSupportedException(
                        string.Format("Func with so many ({0}) parameters is not supported!", paramTypes.Length));
            }
        }

        public static int GetFirstIndex<T>(this IList<T> source, object item)
        {
            if (source == null || source.Count == 0)
                return -1;
            var count = source.Count;
            if (count == 1)
                return ReferenceEquals(source[0], item) ? 0 : -1;
            for (var i = 0; i < count; ++i)
                if (ReferenceEquals(source[i], item))
                    return i;
            return -1;
        }

        public static int GetFirstIndex<T>(this T[] source, Func<T, bool> predicate)
        {
            if (source == null || source.Length == 0)
                return -1;
            if (source.Length == 1)
                return predicate(source[0]) ? 0 : -1;
            for (var i = 0; i < source.Length; ++i)
                if (predicate(source[i]))
                    return i;
            return -1;
        }

        public static T GetFirst<T>(this IEnumerable<T> source)
        {
            var arr = source as T[];
            return arr == null
                ? source.FirstOrDefault()
                : arr.Length != 0 ? arr[0] : default(T);
        }

        public static T GetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var arr = source as T[];
            if (arr == null)
                return source.FirstOrDefault(predicate);
            var index = arr.GetFirstIndex(predicate);
            return index == -1 ? default(T) : arr[index];
        }

        public static R[] Project<T, R>(this T[] source, Func<T, R> project)
        {
            if (source == null || source.Length == 0)
                return Empty<R>();

            if (source.Length == 1)
                return new[] { project(source[0]) };

            var result = new R[source.Length];
            for (var i = 0; i < result.Length; ++i)
                result[i] = project(source[i]);
            return result;
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>Facade for constructing expression info.</summary>
    abstract class ExpressionInfo
    {
        /// <summary>Expression node type.</summary>
        public abstract ExpressionType NodeType { get; }

        /// <summary>All expressions should have a Type.</summary>
        public abstract Type Type { get; }

        /// <summary>Converts back to respective expression so you may Compile it by usual means.</summary>
        public abstract Expression ToExpression();

        /// <summary>Converts to Expression and outputs its as string</summary>
        public override string ToString() => ToExpression().ToString();

        /// <summary>Analog of Expression.Parameter</summary>
        /// <remarks>For now it is return just an `Expression.Parameter`</remarks>
        public static ParameterExpressionInfo Parameter(Type type, string name = null) =>
            new ParameterExpressionInfo(type, name);

        /// <summary>Analog of Expression.Constant</summary>
        public static ConstantExpressionInfo Constant(object value, Type type = null) =>
            value == null && type == null ? _nullExprInfo
                : new ConstantExpressionInfo(value, type ?? value.GetType());

        private static readonly ConstantExpressionInfo
            _nullExprInfo = new ConstantExpressionInfo(null, typeof(object));

        /// <summary>Analog of Expression.New</summary>
        public static NewExpressionInfo New(ConstructorInfo ctor) =>
            new NewExpressionInfo(ctor, Tools.Empty<object>());

        /// <summary>Analog of Expression.New</summary>
        public static NewExpressionInfo New(ConstructorInfo ctor, params object[] arguments) =>
            new NewExpressionInfo(ctor, arguments);

        /// <summary>Analog of Expression.New</summary>
        public static NewExpressionInfo New(ConstructorInfo ctor, params ExpressionInfo[] arguments) =>
            new NewExpressionInfo(ctor, arguments);

        /// <summary>Static method call</summary>
        public static MethodCallExpressionInfo Call(MethodInfo method, params object[] arguments) =>
            new MethodCallExpressionInfo(null, method, arguments);

        /// <summary>Static method call</summary>
        public static MethodCallExpressionInfo Call(MethodInfo method, params ExpressionInfo[] arguments) =>
            new MethodCallExpressionInfo(null, method, arguments);

        /// <summary>Instance method call</summary>
        public static MethodCallExpressionInfo Call(
            ExpressionInfo instance, MethodInfo method, params object[] arguments) =>
            new MethodCallExpressionInfo(instance, method, arguments);

        /// <summary>Instance method call</summary>
        public static MethodCallExpressionInfo Call(
            ExpressionInfo instance, MethodInfo method, params ExpressionInfo[] arguments) =>
            new MethodCallExpressionInfo(instance, method, arguments);

        /// <summary>Static property</summary>
        public static PropertyExpressionInfo Property(PropertyInfo property) =>
            new PropertyExpressionInfo(null, property);

        /// <summary>Instance property</summary>
        public static PropertyExpressionInfo Property(ExpressionInfo instance, PropertyInfo property) =>
            new PropertyExpressionInfo(instance, property);

        /// <summary>Instance property</summary>
        public static PropertyExpressionInfo Property(object instance, PropertyInfo property) =>
            new PropertyExpressionInfo(instance, property);

        /// <summary>Static field</summary>
        public static FieldExpressionInfo Field(FieldInfo field) =>
            new FieldExpressionInfo(null, field);

        /// <summary>Instance field</summary>
        public static FieldExpressionInfo Field(ExpressionInfo instance, FieldInfo field) =>
            new FieldExpressionInfo(instance, field);

        /// <summary>Analog of Expression.Lambda</summary>
        public static LambdaExpressionInfo Lambda(ExpressionInfo body) =>
            new LambdaExpressionInfo(null, body, Tools.Empty<object>());

        /// <summary>Analog of Expression.Lambda</summary>
        public static LambdaExpressionInfo Lambda(ExpressionInfo body,
            params ParameterExpression[] parameters) =>
            new LambdaExpressionInfo(null, body, parameters);

        /// <summary>Analog of Expression.Lambda</summary>
        public static LambdaExpressionInfo Lambda(object body, params object[] parameters) =>
            new LambdaExpressionInfo(null, body, parameters);

        /// <summary>Analog of Expression.Lambda with lambda type specified</summary>
        public static LambdaExpressionInfo Lambda(Type delegateType, object body, params object[] parameters) =>
            new LambdaExpressionInfo(delegateType, body, parameters);

        /// <summary>Analog of Expression.Convert</summary>
        public static UnaryExpressionInfo Convert(ExpressionInfo operand, Type targetType) =>
            new UnaryExpressionInfo(ExpressionType.Convert, operand, targetType);

        /// <summary>Analog of Expression.Lambda</summary>
        public static ExpressionInfo<TDelegate> Lambda<TDelegate>(ExpressionInfo body) =>
            new ExpressionInfo<TDelegate>(body, Tools.Empty<ParameterExpression>());

        /// <summary>Analog of Expression.Lambda</summary>
        public static ExpressionInfo<TDelegate> Lambda<TDelegate>(ExpressionInfo body,
            params ParameterExpression[] parameters) =>
            new ExpressionInfo<TDelegate>(body, parameters);

        /// <summary>Analog of Expression.Lambda</summary>
        public static ExpressionInfo<TDelegate> Lambda<TDelegate>(ExpressionInfo body,
            params ParameterExpressionInfo[] parameters) =>
            new ExpressionInfo<TDelegate>(body, parameters);

        /// <summary>Analog of Expression.ArrayIndex</summary>
        public static BinaryExpressionInfo ArrayIndex(ExpressionInfo array, ExpressionInfo index) =>
            new ArrayIndexExpressionInfo(array, index, array.Type.GetElementType());

        /// <summary>Analog of Expression.ArrayIndex</summary>
        public static BinaryExpressionInfo ArrayIndex(object array, object index) =>
            new ArrayIndexExpressionInfo(array, index, array.GetResultType().GetElementType());

        /// <summary>Expression.Bind used in Expression.MemberInit</summary>
        public static MemberAssignmentInfo Bind(MemberInfo member, ExpressionInfo expression) =>
            new MemberAssignmentInfo(member, expression);

        /// <summary>Analog of Expression.MemberInit</summary>
        public static MemberInitExpressionInfo MemberInit(NewExpressionInfo newExpr,
            params MemberAssignmentInfo[] bindings) =>
            new MemberInitExpressionInfo(newExpr, bindings);

        /// <summary>Enables member assignment on existing instance expression.</summary>
        public static ExpressionInfo MemberInit(ExpressionInfo instanceExpr,
            params MemberAssignmentInfo[] assignments) =>
            new MemberInitExpressionInfo(instanceExpr, assignments);

        /// <summary>Constructs an array given the array type and item initializer expressions.</summary>
        public static NewArrayExpressionInfo NewArrayInit(Type type, params object[] initializers) =>
            new NewArrayExpressionInfo(type, initializers);

        /// <summary>Constructs an array given the array type and item initializer expressions.</summary>
        public static NewArrayExpressionInfo NewArrayInit(Type type, params ExpressionInfo[] initializers) =>
            new NewArrayExpressionInfo(type, initializers);

        /// <summary>Constructs assignment expression.</summary>
        public static ExpressionInfo Assign(ExpressionInfo left, ExpressionInfo right) =>
            new AssignBinaryExpressionInfo(left, right, left.Type);

        /// <summary>Constructs assignment expression from possibly mixed types of left and right.</summary>
        public static ExpressionInfo Assign(object left, object right) =>
            new AssignBinaryExpressionInfo(left, right, left.GetResultType());

        /// <summary>Invoke</summary>
        public static ExpressionInfo Invoke(ExpressionInfo lambda, params object[] args) =>
            new InvocationExpressionInfo(lambda, args, lambda.Type);

        /// <summary>Binary add</summary>
        public static ExpressionInfo Add(ExpressionInfo left, ExpressionInfo right) =>
            new ArithmeticBinaryExpressionInfo(ExpressionType.Add, left, right, left.Type);

        /// <summary>Binary substract</summary>
        public static ExpressionInfo Substract(ExpressionInfo left, ExpressionInfo right) =>
            new ArithmeticBinaryExpressionInfo(ExpressionType.Subtract, left, right, left.Type);

        public static ExpressionInfo Multiply(ExpressionInfo left, ExpressionInfo right) =>
            new ArithmeticBinaryExpressionInfo(ExpressionType.Multiply, left, right, left.Type);

        public static ExpressionInfo Divide(ExpressionInfo left, ExpressionInfo right) =>
            new ArithmeticBinaryExpressionInfo(ExpressionType.Divide, left, right, left.Type);

        public static BlockExpressionInfo Block(params object[] expressions) =>
            new BlockExpressionInfo(expressions[expressions.Length - 1].GetResultType(),
                Tools.Empty<ParameterExpressionInfo>(), expressions);

        public static TryExpressionInfo TryCatch(object body, params CatchBlockInfo[] handlers) =>
            new TryExpressionInfo(body, null, handlers);

        public static TryExpressionInfo TryCatchFinally(object body, ExpressionInfo @finally, params CatchBlockInfo[] handlers) =>
            new TryExpressionInfo(body, @finally, handlers);

        public static TryExpressionInfo TryFinally(object body, ExpressionInfo @finally) =>
            new TryExpressionInfo(body, @finally, null);

        public static CatchBlockInfo Catch(ParameterExpressionInfo variable, ExpressionInfo body) =>
            new CatchBlockInfo(variable, body, null, variable.Type);

        public static CatchBlockInfo Catch(Type test, ExpressionInfo body) =>
            new CatchBlockInfo(null, body, null, test);

        public static UnaryExpressionInfo Throw(ExpressionInfo value) =>
            new UnaryExpressionInfo(ExpressionType.Throw, value, typeof(void));
    }

    class UnaryExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public readonly ExpressionInfo Operand;

        public override Expression ToExpression()
        {
            if (NodeType == ExpressionType.Convert)
                return Expression.Convert(Operand.ToExpression(), Type);
            throw new NotSupportedException("Cannot convert ExpressionInfo to Expression of type " + NodeType);
        }

        public UnaryExpressionInfo(ExpressionType nodeType, ExpressionInfo operand, Type type)
        {
            NodeType = nodeType;
            Operand = operand;
            Type = type;
        }
    }

    abstract class BinaryExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public readonly object Left, Right;

        protected BinaryExpressionInfo(ExpressionType nodeType, object left, object right, Type type)
        {
            NodeType = nodeType;
            Type = type;
            Left = left;
            Right = right;
        }
    }

    class ArithmeticBinaryExpressionInfo : BinaryExpressionInfo
    {
        public ArithmeticBinaryExpressionInfo(ExpressionType nodeType, object left, object right, Type type)
            : base(nodeType, left, right, type) { }

        public override Expression ToExpression()
        {
            if (NodeType == ExpressionType.Add)
                return Expression.Add(Left.ToExpression(), Right.ToExpression());
            if (NodeType == ExpressionType.Subtract)
                return Expression.Subtract(Left.ToExpression(), Right.ToExpression());
            if (NodeType == ExpressionType.Multiply)
                return Expression.Multiply(Left.ToExpression(), Right.ToExpression());
            if (NodeType == ExpressionType.Divide)
                return Expression.Divide(Left.ToExpression(), Right.ToExpression());
            throw new NotSupportedException($"Not valid {NodeType} for arithmetic binary expression.");
        }
    }

    class ArrayIndexExpressionInfo : BinaryExpressionInfo
    {
        public ArrayIndexExpressionInfo(object left, object right, Type type)
            : base(ExpressionType.ArrayIndex, left, right, type) { }

        public override Expression ToExpression() =>
            Expression.ArrayIndex(Left.ToExpression(), Right.ToExpression());
    }

    class AssignBinaryExpressionInfo : BinaryExpressionInfo
    {
        public AssignBinaryExpressionInfo(object left, object right, Type type)
            : base(ExpressionType.Assign, left, right, type) { }

        public override Expression ToExpression() =>
            Expression.Assign(Left.ToExpression(), Right.ToExpression());
    }

    class MemberInitExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.MemberInit;
        public override Type Type => ExpressionInfo.Type;

        public NewExpressionInfo NewExpressionInfo => ExpressionInfo as NewExpressionInfo;

        public readonly ExpressionInfo ExpressionInfo;
        public readonly MemberAssignmentInfo[] Bindings;

        public override Expression ToExpression() =>
            Expression.MemberInit(NewExpressionInfo.ToNewExpression(),
                Bindings.Project(b => b.ToMemberAssignment()));

        public MemberInitExpressionInfo(NewExpressionInfo newExpressionInfo, MemberAssignmentInfo[] bindings)
            : this((ExpressionInfo)newExpressionInfo, bindings) { }

        public MemberInitExpressionInfo(ExpressionInfo expressionInfo, MemberAssignmentInfo[] bindings)
        {
            ExpressionInfo = expressionInfo;
            Bindings = bindings ?? Tools.Empty<MemberAssignmentInfo>();
        }
    }

    class ParameterExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Parameter;
        public override Type Type { get; }

        public readonly string Name;

        public override Expression ToExpression() => ParamExpr;

        public ParameterExpression ParamExpr =>
            _parameter ?? (_parameter = Expression.Parameter(Type, Name));

        public static implicit operator ParameterExpression(ParameterExpressionInfo info) => info.ParamExpr;

        public ParameterExpressionInfo(Type type, string name)
        {
            Type = type;
            Name = name;
        }

        public ParameterExpressionInfo(ParameterExpression paramExpr)
            : this(paramExpr.Type, paramExpr.Name)
        {
            _parameter = paramExpr;
        }

        private ParameterExpression _parameter;
    }

    class ConstantExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Constant;
        public override Type Type { get; }

        public readonly object Value;

        public override Expression ToExpression() => Expression.Constant(Value, Type);

        public ConstantExpressionInfo(object value, Type type)
        {
            Value = value;
            Type = type;
        }
    }

    abstract class ArgumentsExpressionInfo : ExpressionInfo
    {
        public readonly object[] Arguments;

        protected Expression[] ArgumentsToExpressions() => Arguments.Project(Tools.ToExpression);

        protected ArgumentsExpressionInfo(object[] arguments)
        {
            Arguments = arguments ?? Tools.Empty<object>();
        }
    }

    class NewExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.New;
        public override Type Type => Constructor.DeclaringType;

        public readonly ConstructorInfo Constructor;

        public override Expression ToExpression() => ToNewExpression();

        public NewExpression ToNewExpression() => Expression.New(Constructor, ArgumentsToExpressions());

        public NewExpressionInfo(ConstructorInfo constructor, params object[] arguments) : base(arguments)
        {
            Constructor = constructor;
        }
    }

    class NewArrayExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.NewArrayInit;
        public override Type Type { get; }

        // todo: That it is a ReadOnlyCollection<Expression> in original NewArrayExpression. 
        // I made it a ICollection for now to use Arguments as input, without changing Arguments type
        public ICollection<object> Expressions => Arguments;

        public override Expression ToExpression() =>
            Expression.NewArrayInit(_elementType, ArgumentsToExpressions());

        public NewArrayExpressionInfo(Type elementType, object[] elements) : base(elements)
        {
            Type = elementType.MakeArrayType();
            _elementType = elementType;
        }

        private readonly Type _elementType;
    }

    class MethodCallExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Call;
        public override Type Type => Method.ReturnType;

        public readonly MethodInfo Method;
        public readonly ExpressionInfo Object;

        public override Expression ToExpression() =>
            Expression.Call(Object?.ToExpression(), Method, ArgumentsToExpressions());

        public MethodCallExpressionInfo(ExpressionInfo @object, MethodInfo method, params object[] arguments)
            : base(arguments)
        {
            Object = @object;
            Method = method;
        }
    }

    abstract class MemberExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.MemberAccess;
        public readonly MemberInfo Member;

        public readonly object Expression;

        protected MemberExpressionInfo(object expression, MemberInfo member)
        {
            Expression = expression;
            Member = member;
        }
    }

    class PropertyExpressionInfo : MemberExpressionInfo
    {
        public override Type Type => PropertyInfo.PropertyType;
        public PropertyInfo PropertyInfo => (PropertyInfo)Member;

        public override Expression ToExpression() =>
            System.Linq.Expressions.Expression.Property(Expression.ToExpression(), PropertyInfo);

        public PropertyExpressionInfo(object instance, PropertyInfo property)
            : base(instance, property) { }
    }

    class FieldExpressionInfo : MemberExpressionInfo
    {
        public override Type Type => FieldInfo.FieldType;
        public FieldInfo FieldInfo => (FieldInfo)Member;

        public override Expression ToExpression() =>
            System.Linq.Expressions.Expression.Field(Expression.ToExpression(), FieldInfo);

        public FieldExpressionInfo(ExpressionInfo instance, FieldInfo field)
            : base(instance, field) { }
    }

    struct MemberAssignmentInfo
    {
        public MemberInfo Member;
        public ExpressionInfo Expression;

        public MemberBinding ToMemberAssignment() =>
            System.Linq.Expressions.Expression.Bind(Member, Expression.ToExpression());

        public MemberAssignmentInfo(MemberInfo member, ExpressionInfo expression)
        {
            Member = member;
            Expression = expression;
        }
    }

    class InvocationExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Invoke;
        public override Type Type { get; }

        public readonly ExpressionInfo ExprToInvoke;

        public override Expression ToExpression() =>
            Expression.Invoke(ExprToInvoke.ToExpression(), ArgumentsToExpressions());

        public InvocationExpressionInfo(ExpressionInfo exprToInvoke, object[] arguments, Type type) : base(arguments)
        {
            ExprToInvoke = exprToInvoke;
            Type = type;
        }
    }

    class BlockExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Block;

        public override Type Type { get; }

        public readonly ParameterExpressionInfo[] Variables;
        public readonly object[] Expressions;
        public readonly object Result;

        public override Expression ToExpression() =>
            Expression.Block(Expressions.Project(Tools.ToExpression));

        public BlockExpressionInfo(Type type, ParameterExpressionInfo[] variables, object[] expressions)
        {
            Variables = variables;
            Expressions = expressions;
            Result = expressions[expressions.Length - 1];
            Type = type;
        }
    }

    class TryExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Try;
        public override Type Type { get; }

        public readonly object Body;
        public readonly CatchBlockInfo[] Handlers;
        public readonly ExpressionInfo Finally;

        public override Expression ToExpression() =>
            Finally == null ? Expression.TryCatch(Body.ToExpression(), ToCatchBlocks(Handlers)) :
            Handlers == null ? Expression.TryFinally(Body.ToExpression(), Finally.ToExpression()) :
            Expression.TryCatchFinally(Body.ToExpression(), Finally.ToExpression(), ToCatchBlocks(Handlers));

        private static CatchBlock[] ToCatchBlocks(CatchBlockInfo[] hs)
        {
            if (hs == null)
                return Tools.Empty<CatchBlock>();
            var catchBlocks = new CatchBlock[hs.Length];
            for (var i = 0; i < hs.Length; ++i)
                catchBlocks[i] = hs[i].ToCatchBlock();
            return catchBlocks;
        }

        public TryExpressionInfo(object body, ExpressionInfo @finally, CatchBlockInfo[] handlers)
        {
            Type = body.GetResultType();
            Body = body;
            Handlers = handlers;
            Finally = @finally;
        }
    }

    sealed class CatchBlockInfo
    {
        public readonly ParameterExpressionInfo Variable;
        public readonly ExpressionInfo Body;
        public readonly ExpressionInfo Filter;
        public readonly Type Test;

        public CatchBlockInfo(ParameterExpressionInfo variable, ExpressionInfo body, ExpressionInfo filter, Type test)
        {
            Variable = variable;
            Body = body;
            Filter = filter;
            Test = test;
        }

        public CatchBlock ToCatchBlock() => Expression.Catch(Variable.ParamExpr, Body.ToExpression());
    }

    class LambdaExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Lambda;
        public override Type Type { get; }

        public readonly object Body;
        public object[] Parameters => Arguments;

        public override Expression ToExpression() => ToLambdaExpression();

        public LambdaExpression ToLambdaExpression() =>
            Expression.Lambda(Body.ToExpression(),
                Parameters.Project(p => (ParameterExpression)p.ToExpression()));

        public LambdaExpressionInfo(Type delegateType, object body, object[] parameters) : base(parameters)
        {
            Body = body;
            var bodyType = body.GetResultType();
            Type = delegateType != null && delegateType != typeof(Delegate)
                ? delegateType
                : Tools.GetFuncOrActionType(Tools.GetParamExprTypes(parameters), bodyType);
        }
    }

    class ExpressionInfo<TDelegate> : LambdaExpressionInfo
    {
        public Type DelegateType => Type;

        public override Expression ToExpression() => ToLambdaExpression();

        public new Expression<TDelegate> ToLambdaExpression() =>
            Expression.Lambda<TDelegate>(Body.ToExpression(),
                Parameters.Project(p => (ParameterExpression)p.ToExpression()));

        public ExpressionInfo(ExpressionInfo body, object[] parameters)
            : base(typeof(TDelegate), body, parameters) { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}