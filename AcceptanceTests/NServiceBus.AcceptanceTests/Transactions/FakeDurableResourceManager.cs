namespace NServiceBus.AcceptanceTests.Transactions
{
    using System;
    using System.Transactions;

    public class FakeDurableResourceManager : IEnlistmentNotification
    {
        public static Guid ResourceManagerId = Guid.Parse("6f057e24-a0d8-4c95-b091-b8dc9a916fa4");

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}