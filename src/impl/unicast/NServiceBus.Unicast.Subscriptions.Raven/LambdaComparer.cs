using System;
using System.Collections.Generic;

namespace NServiceBus.Unicast.Subscriptions.Raven
{
    public class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> lambdaComparer;
        private readonly Func<T, int> lambdaHash;

        public LambdaComparer(Func<T, T, bool> lambdaComparer) :
            this(lambdaComparer, o => 0)
        {
        }

        public LambdaComparer(Func<T, T, bool> lambdaComparer, Func<T, int> lambdaHash)
        {
            if (lambdaComparer == null)
                throw new ArgumentNullException("lambdaComparer");
            if (lambdaHash == null)
                throw new ArgumentNullException("lambdaHash");

            this.lambdaComparer = lambdaComparer;
            this.lambdaHash = lambdaHash;
        }

        public bool Equals(T x, T y)
        {
            return lambdaComparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return lambdaHash(obj);
        }
    }
}