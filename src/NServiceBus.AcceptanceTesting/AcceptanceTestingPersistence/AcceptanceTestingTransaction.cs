namespace NServiceBus.AcceptanceTesting;

using System;
using System.Collections.Generic;

class AcceptanceTestingTransaction
{
    public void Enlist(Action action)
    {
        actions.Add(action);
    }

    public void Commit()
    {
        foreach (var action in actions)
        {
            action();
        }
        actions.Clear();
    }

    public void Rollback()
    {
        actions.Clear();
    }

    List<Action> actions = [];
}