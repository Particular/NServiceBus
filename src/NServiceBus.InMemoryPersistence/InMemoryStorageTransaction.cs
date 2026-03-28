namespace NServiceBus.Persistence.InMemory;

using System;
using System.Collections.Generic;

class InMemoryStorageTransaction
{
    public void Enlist(Action action) => enlistedActions.Add(action);

    public void Commit()
    {
        foreach (var action in enlistedActions)
        {
            action();
        }
        enlistedActions.Clear();
    }

    public void Rollback() => enlistedActions.Clear();

    readonly List<Action> enlistedActions = [];
}
