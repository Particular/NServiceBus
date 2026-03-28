namespace NServiceBus.Persistence.InMemory;

using System;
using System.Collections.Generic;

class InMemoryStorageTransaction
{
    public void Enlist(Func<Action?> operation) => enlistedOperations.Add(operation);

    public void Commit()
    {
        var rollbackActions = new Stack<Action>();

        try
        {
            foreach (var operation in enlistedOperations)
            {
                if (operation() is { } rollbackAction)
                {
                    rollbackActions.Push(rollbackAction);
                }
            }
        }
        catch
        {
            while (rollbackActions.TryPop(out var rollbackAction))
            {
                rollbackAction();
            }

            throw;
        }
        finally
        {
            enlistedOperations.Clear();
        }
    }

    public void Rollback() => enlistedOperations.Clear();

    readonly List<Func<Action?>> enlistedOperations = [];
}