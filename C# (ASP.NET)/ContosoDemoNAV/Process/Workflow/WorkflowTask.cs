using System.Collections.Generic;
using System.Linq;

namespace ContosoDemoNAV.Process.Workflow
{
    public class WorkflowTask : IWorkflowTask
    {
        private readonly object _lockPredecessors = new object();
        private readonly List<ITask> _predecessors = new List<ITask>();
        private readonly List<ITask> _successors = new List<ITask>();

        public void RegisterPredecessor(ITask task)
        {
            if (task != null)
                lock (_lockPredecessors)
                    _predecessors.Add(task);
        }

        public void CompletePredecessor(ITask task)
        {
            lock (_lockPredecessors)
                _predecessors.Remove(task);
        }

        public bool HasPredecessors()
        {
            lock (_lockPredecessors)
                return _predecessors.Any();
        }

        public bool IsPredecessorOf(ITask task) => _successors.Contains(task);

        public void RegisterSuccessor(ITask task)
        {
            if (task != null)
                _successors.Add(task);
        }

        public void ReportTasks(List<ITask> tasks)
        {
            lock (_lockPredecessors)
                tasks.AddRange(_predecessors);
            tasks.AddRange(_successors);
        }
    }
}