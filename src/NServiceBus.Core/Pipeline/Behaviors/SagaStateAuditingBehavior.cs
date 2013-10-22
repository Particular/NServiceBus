namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This one would be a plugin somehow
    /// </summary>
    class SagaStateAuditingBehavior : IBehavior,
        RequireContextItemOfType<ActiveSagaInstances>
    {
        public void Invoke(BehaviorContext context, Action next)
        {
            // let's just start out by handling one single saga instance
            var snapshotsBefore = context.Get<ActiveSagaInstances>()
                                         .Instances
                                         .Select(AsNewSnapshot)
                                         .ToList();

            next();


            var snapshotsAfter = context.Get<ActiveSagaInstances>()
                                        .Instances
                                        .Select(AsNewSnapshot)
                                        .ToList();

            var pairs = GetPairs(snapshotsBefore, snapshotsAfter);

            foreach (var pair in pairs)
            {
                //switch (pair.ChangeDescriptor)
                //{
                //    case SagaInstanceStateChangeDescriptor.New:
                //        //ReportToServiceControl(new SagaInstanceCreated(id, type, after));
                //        break;
                //    case SagaInstanceStateChangeDescriptor.Existing:
                //        //ReportToServiceControl(new SagaInstanceUpdated(id, type, diff));
                //        break;
                //    case SagaInstanceStateChangeDescriptor.Completed:
                //        //ReportToServiceControl(new SagaInstanceCompleted(id, type, diff));
                //        //< diff? persister might just "archive" the data or something
                //        break;
                //    default:
                //        throw new ArgumentOutOfRangeException();
                //}
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

           
        }

        Snapshot AsNewSnapshot(ActiveSagaInstance container)
        {
            throw new NotImplementedException();
        }
    }
}