namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Implementation provided by the infrastructure - don't implement this
    /// or register implementations of it in the container unless you intend
    /// to substantially change the way sagas work.
    /// </summary>
    public interface IConfigureHowToFindSagaWithMessage
    {
        /// <summary>
        /// Specify that when the infrastructure is handling a message 
        /// of the given type, which message property should be matched to 
        /// which saga entity property in the persistent saga store.
        /// </summary>
        void ConfigureMapping<TSagaEntity, TMessage, TSagaIdentifier>(Expression<Func<TSagaEntity, TSagaIdentifier>> sagaEntityProperty, Expression<Func<TMessage, TSagaIdentifier>> messageProperty) where TSagaEntity : IContainSagaData;
    }
}