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
            return new NonEmptyEnumerable<T>(source, exceptionMessage);
        }

        class NonEmptyEnumerable<T> : IEnumerable<T>
        {
            IEnumerable<T> source;
            Func<string> exceptionMessage;

            public NonEmptyEnumerable(IEnumerable<T> source, Func<string> exceptionMessage)
            {
                this.source = source;
                this.exceptionMessage = exceptionMessage;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new NonEmptyEnumerator(source.GetEnumerator(), exceptionMessage);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            [SkipWeaving]
            class NonEmptyEnumerator : IEnumerator<T>
            {
                IEnumerator<T> source;
                Func<string> exceptionMessage;
                bool enumerationStarted;

                public NonEmptyEnumerator(IEnumerator<T> source, Func<string> exceptionMessage)
                {
                    this.source = source;
                    this.exceptionMessage = exceptionMessage;
                }

                public void Dispose()
                {
                    source.Dispose();
                }

                public bool MoveNext()
                {
                    var result = source.MoveNext();
                    if (!enumerationStarted && !result)
                    {
                        throw new Exception(exceptionMessage());
                    }
                    enumerationStarted = true;
                    return result;
                }

                public void Reset()
                {
                    source.Reset();
                }

                public T Current { get { return source.Current; }}

                object IEnumerator.Current
                {
                    get { return Current; }
                }
            }
        }
    }
}