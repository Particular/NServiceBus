namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This one would be a plugin somehow
    /// </summary>
    class SagaStateAuditingBehavior : IBehavior,
        ExecuteAfter<SagaPersistenceBehavior>,
        ExecuteBefore<InvokeHandlersBehavior>,
        RequireContextItemOfType<ActiveSagaInstances>
    {
        public IBehavior Next { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            // let's just start out by handling one single saga instance
            var snapshotsBefore = context.Get<ActiveSagaInstances>()
                                         .Instances
                                         .Select(AsNewSnapshot)
                                         .ToList();

            Next.Invoke(context);


            var snapshotsAfter = context.Get<ActiveSagaInstances>()
                                        .Instances
                                        .Select(AsNewSnapshot)
                                        .ToList();

            var pairs = GetPairs(snapshotsBefore, snapshotsAfter);

            foreach (var pair in pairs)
            {
                switch (pair.ChangeDescriptor)
                {
                    case SagaInstanceStateChangeDescriptor.New:
                        //ReportToServiceControl(new SagaInstanceCreated(id, type, after));
                        break;
                    case SagaInstanceStateChangeDescriptor.Existing:
                        //ReportToServiceControl(new SagaInstanceUpdated(id, type, diff));
                        break;
                    case SagaInstanceStateChangeDescriptor.Completed:
                        //ReportToServiceControl(new SagaInstanceCompleted(id, type, diff));
                        //< diff? persister might just "archive" the data or something
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        IEnumerable<SnapshotPair> GetPairs(List<Snapshot> snapshotsBefore, List<Snapshot> snapshotsAfter)
        {
            var sagaIds = snapshotsBefore.Select(s => s.SagaId)
                                         .Union(snapshotsAfter.Select(s => s.SagaId))
                                         .Distinct();

            return sagaIds
                .Select(id => new SnapshotPair
                                  {
                                      Before = snapshotsBefore.FirstOrDefault(s => s.SagaId == id),
                                      After = snapshotsAfter.FirstOrDefault(s => s.SagaId == id),
                                  });
        }

        class SnapshotPair
        {
            public Snapshot Before { get; set; }
            public Snapshot After { get; set; }

            public SagaInstanceStateChangeDescriptor ChangeDescriptor
            {
                get { return SagaInstanceStateChangeDescriptor.Existing; }
            }
        }

        Snapshot AsNewSnapshot(SagaInstanceContainer container)
        {
            throw new NotImplementedException();
        }
    }

    class Snapshot
    {
        public Guid SagaId { get; set; }
    }

    /// <summary>
    /// invoke handler'n'stuff
    /// </summary>
    public class InvokeHandlersBehavior : IBehavior
    {
        public IBehavior Next { get; set; }
        public void Invoke(IBehaviorContext context)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// could we declaratively state context state requirements like this? (and maybe in some cases remove the need for ExecuteAfter<..>?)
    /// </summary>
    public interface RequireContextItemOfType<T>
    {
    }

    /// <summary>
    /// Could be state behavior chain position requirements like this?
    /// </summary>
    public interface ExecuteBefore<T>
    {
    }

    /// <summary>
    /// Could be state behavior chain position requirements like this?
    /// </summary>
    public interface ExecuteAfter<T>
    {
    }
}