namespace NServiceBus.ObjectBuilder.Spring
{
    using System;
    using global::Spring.Objects.Factory;

    class ArbitraryFuncDelegatingFactoryObject<T> : IFactoryObject
    {
        bool isSingleton;
        Func<T> builderDelegate;


        public ArbitraryFuncDelegatingFactoryObject(Func<T> builderDelegate, bool isSingleton)
        {
            this.builderDelegate = builderDelegate;
            this.isSingleton = isSingleton;
        }


        /// <summary>
        /// Return an instance (possibly shared or independent) of the object managed by this factory.
        /// </summary>
        /// <remarks>
        /// <note type="caution">If this method is being called in the context of an enclosing IoC container and returns <see langword="null"/>, the IoC container will consider this factory object as not being fully initialized and throw a corresponding (and most probably fatal) exception.</note>
        /// </remarks>
        /// <returns>
        /// An instance (possibly shared or independent) of the object managed by this factory.
        /// </returns>
        public object GetObject()
        {
            return builderDelegate.Invoke();
        }

        /// <summary>
        /// Return the <see cref="Type"/> of object that this <see cref="IFactoryObject"/> creates, or <see langword="null"/> if not known in advance.
        /// </summary>
        public Type ObjectType
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// Is the object managed by this factory a singleton or a prototype?
        /// </summary>
        public bool IsSingleton
        {
            get { return isSingleton; }
        }
    }
}