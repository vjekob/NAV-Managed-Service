using System.Collections.Generic;

namespace ContosoDemoNAV.Process.Workflow
{
    public interface IWorkflowTask
    {
        void RegisterPredecessor(ITask task);
        void CompletePredecessor(ITask task);
        bool HasPredecessors();
        bool IsPredecessorOf(ITask task);
        void RegisterSuccessor(ITask task);

        void ReportTasks(List<ITask> task);
    }
}