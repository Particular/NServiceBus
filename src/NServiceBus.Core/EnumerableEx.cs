namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Janitor;

    static class EnumerableEx
    {
        public static IEnumerable<T> EnsureNonEmpty<T>(this IEnumerable<T> source, Func<string> exceptionMessage)
        {
            return new NonEmptyEnumerable<T>(source, () =>
            {
                throw new Exception(exceptionMessage());
            });
        }

        public static IEnumerable<T> EnsureNonEmpty<T>(this IEnumerable<T> source, Func<T> emptyElement)
        {
            return new NonEmptyEnumerable<T>(source, emptyElement);
        }

        class NonEmptyEnumerable<T> : IEnumerable<T>
        {
            IEnumerable<T> source;
            Func<T> ifEmptyCallback;

            public NonEmptyEnumerable(IEnumerable<T> source, Func<T> ifEmptyCallback)
            {
                this.source = source;
                this.ifEmptyCallback = ifEmptyCallback;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new NonEmptyEnumerator(source.GetEnumerator(), ifEmptyCallback);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            [SkipWeaving]
            class NonEmptyEnumerator : IEnumerator<T>
            {
                IEnumerator<T> source;
                T emptyValue;
                bool isEmpty;
                Func<T> ifEmptyCallback;
                bool enumerationStarted;

                public NonEmptyEnumerator(IEnumerator<T> source, Func<T> ifEmptyCallback)
                {
                    this.source = source;
                    this.ifEmptyCallback = ifEmptyCallback;
                }

                public void Dispose()
                {
                    source.Dispose();
                }

                public bool MoveNext()
                {
                    var result = source.MoveNext();
                    if (!result && !enumerationStarted)
                    {
                        isEmpty = true;
                        emptyValue = ifEmptyCallback();
                        result = true;
                    }
                    enumerationStarted = true;
                    return result;
                }

                public void Reset()
                {
                    source.Reset();
                }

                public T Current => isEmpty ? emptyValue : source.Current;

                object IEnumerator.Current => Current;
            }
        }
    }
}