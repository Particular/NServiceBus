namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Janitor;

    static class EnumerableEx
    {
        public static IEnumerable<T> Single<T>(T singleElement)
        {
            return Enumerable.Repeat(singleElement, 1);
        }

        public static IEnumerable<T> EnsureNonEmpty<T>(this IEnumerable<T> source, Func<string> exceptionMessage)
        {
            return new NonEmptyEnumerable<T>(source, () => { throw new Exception(exceptionMessage()); });
        }

        public static IEnumerable<T> EnsureNonEmpty<T>(this IEnumerable<T> source, Func<T> emptyElement)
        {
            return new NonEmptyEnumerable<T>(source, emptyElement);
        }

        class NonEmptyEnumerable<T> : IEnumerable<T>
        {
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

            Func<T> ifEmptyCallback;
            IEnumerable<T> source;

            [SkipWeaving]
            class NonEmptyEnumerator : IEnumerator<T>
            {
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
                T emptyValue;
                bool enumerationStarted;
                Func<T> ifEmptyCallback;
                bool isEmpty;
                IEnumerator<T> source;
            }
        }
    }
}