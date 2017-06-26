﻿namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Transactions;

    public class FakePromotableResourceManager : IPromotableSinglePhaseNotification, IEnlistmentNotification
    {
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

        public void Initialize()
        {
        }

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            singlePhaseEnlistment.Committed();
        }

        public void Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            singlePhaseEnlistment.Done();
        }

        public byte[] Promote()
        {
#pragma warning disable PC001
            return TransactionInterop.GetTransmitterPropagationToken(new CommittableTransaction());
#pragma warning restore PC001
        }

        public static Guid ResourceManagerId = Guid.Parse("6f057e24-a0d8-4c95-b091-b8dc9a916fa4");
    }
}